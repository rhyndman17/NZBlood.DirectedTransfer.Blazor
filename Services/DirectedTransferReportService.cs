using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using NZBlood.DirectedTransfer.Blazor.Models;
using Syncfusion.HtmlConverter;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;

namespace NZBlood.DirectedTransfer.Blazor.Services;

public sealed class DirectedTransferReportService : IDirectedTransferReportService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<DirectedTransferReportService> _logger;

    public DirectedTransferReportService(IMemoryCache cache, ILogger<DirectedTransferReportService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public Task<GeneratedReportFile> GenerateAsync(DirectedTransferReportRequest request, CancellationToken cancellationToken = default)
    {
        var html = BuildHtml(request);
        var orientation = request.ReportOption == 1
            ? PdfPageOrientation.Portrait
            : PdfPageOrientation.Landscape;
        var content = ConvertHtmlToPdf(html, orientation);
        var fileName = string.IsNullOrWhiteSpace(request.TransferOrderCode)
            ? request.User.UserId + "_DTPick.pdf"
            : request.TransferOrderCode + ".pdf";
        var report = new GeneratedReportFile(Guid.NewGuid().ToString("N"), fileName, content);
        _cache.Set(report.ReportId, report, TimeSpan.FromMinutes(30));
        return Task.FromResult(report);
    }

    private byte[] ConvertHtmlToPdf(string html, PdfPageOrientation orientation)
    {
        try
        {
            var converter = new HtmlToPdfConverter(HtmlRenderingEngine.Blink);
            var settings = new BlinkConverterSettings
            {
                Margin = new PdfMargins { Left = 24, Right = 24, Top = 44, Bottom = 38 },
                Orientation = orientation,
                PdfPageSize = PdfPageSize.A4,
                ViewPortSize = new Syncfusion.Drawing.Size(orientation == PdfPageOrientation.Portrait ? 1000 : 1100, 0)
            };
            settings.CommandLineArguments.Add("--no-sandbox");
            converter.ConverterSettings = settings;

            using var document = converter.Convert(html, string.Empty);
            AddPageFurniture(document);
            using var stream = new MemoryStream();
            document.Save(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Syncfusion HTML-to-PDF conversion failed; generating a simple fallback PDF.");
            using var document = new PdfDocument();
            var page = document.Pages.Add();
            var graphics = page.Graphics;
            var font = new PdfStandardFont(PdfFontFamily.Helvetica, 11);
            graphics.DrawString("Directed Transfer report generation failed. See application logs for details.", font, PdfBrushes.Black, 24, 24);
            using var stream = new MemoryStream();
            document.Save(stream);
            return stream.ToArray();
        }
    }

    private static void AddPageFurniture(PdfDocument document)
    {
        string[] headings = ["Zone", "Priority", "GP Item Code", "Vendor Item", "Description", "UOM", "UOM Description", "Order Up To", "Qty To Order"];
        float[] widths = [0.05f, 0.06f, 0.12f, 0.10f, 0.27f, 0.06f, 0.19f, 0.07f, 0.08f];
        var headerFont = new PdfStandardFont(PdfFontFamily.Helvetica, 8, PdfFontStyle.Bold);
        var pageFont = new PdfStandardFont(PdfFontFamily.Helvetica, 9);
        var totalPages = document.Pages.Count;

        for (var pageIndex = 0; pageIndex < totalPages; pageIndex++)
        {
            var page = document.Pages[pageIndex];
            var graphics = page.Graphics;
            var clientSize = graphics.ClientSize;

            if (pageIndex > 0)
            {
                var contentLeft = 24f;
                var contentWidth = clientSize.Width - 48f;
                var columnLeft = contentLeft;

                for (var columnIndex = 0; columnIndex < headings.Length; columnIndex++)
                {
                    var columnWidth = contentWidth * widths[columnIndex];
                    graphics.DrawString(
                        headings[columnIndex].ToUpperInvariant(),
                        headerFont,
                        PdfBrushes.Black,
                        new Syncfusion.Drawing.RectangleF(columnLeft, 12, columnWidth, 24));
                    columnLeft += columnWidth;
                }

                graphics.DrawLine(PdfPens.Gray, contentLeft, 36, clientSize.Width - 24, 36);
            }

            var pageText = $"Page {pageIndex + 1} of {totalPages}";
            var textSize = pageFont.MeasureString(pageText);
            graphics.DrawString(
                pageText,
                pageFont,
                PdfBrushes.Black,
                clientSize.Width - 24 - textSize.Width,
                clientSize.Height - 24);
        }
    }

    private static string BuildHtml(DirectedTransferReportRequest request)
    {
        var rows = request.ReportOption == 1
            ? request.Items
            : request.Items.Where(item => item.QtyToOrder != 0).ToList();
        var title = request.ReportOption == 1 ? "Directed Transfer Pick List" : "Directed Transfer Processed Order";

        var html = new StringBuilder();
        html.AppendLine("""
            <!doctype html>
            <html>
            <head>
            <meta charset="utf-8">
            <style>
            body { color: #201f1e; font-family: 'Segoe UI', Arial, sans-serif; font-size: 14px; margin: 0; }
            .header { align-items: start; border-bottom: 2px solid #a4262c; display: flex; justify-content: space-between; padding-bottom: 12px; }
            h1 { font-size: 24px; margin: 0 0 6px; }
            .meta { display: grid; gap: 4px; grid-template-columns: 120px 1fr; margin-top: 10px; }
            .meta span:nth-child(odd) { color: #605e5c; font-weight: 700; text-transform: uppercase; }
            .brand { color: #a4262c; font-size: 18px; font-weight: 800; text-align: right; }
            table { border-collapse: collapse; margin-top: 16px; table-layout: fixed; width: 100%; }
            thead { display: table-header-group; }
            tbody { display: table-row-group; }
            tr { break-inside: avoid; page-break-inside: avoid; }
            th { background: #ffffff; border-bottom: 1px solid #c8c6c4; color: #323130; font-family: Arial, sans-serif; font-size: 8pt; padding: 6px 4px; text-align: left; text-transform: uppercase; }
            td { border-bottom: 1px solid #edebe9; font-size: 14px; padding: 6px 4px; vertical-align: top; }
            .code { overflow-wrap: anywhere; }
            .text { overflow-wrap: anywhere; word-break: normal; }
            .num { padding-left: 3px; padding-right: 3px; text-align: right; white-space: nowrap; }
            .center-num { padding-left: 3px; padding-right: 3px; text-align: center; white-space: nowrap; }
            .write-in-cell { min-height: 2.4em; position: relative; }
            .write-in { border-bottom: 1px solid #201f1e; bottom: 6px; display: block; left: 4px; position: absolute; right: 4px; }
            .center { text-align: center; white-space: nowrap; }
            .pending { color: #a4262c; font-weight: 700; }
            </style>
            </head>
            <body>
            """);

        html.AppendLine("<section class=\"header\">");
        html.AppendLine("<div>");
        html.AppendLine("<h1>" + Html(title) + "</h1>");
        html.AppendLine("<div class=\"meta\">");
        AddMeta(html, "Transfer", request.TransferOrderCode ?? "Preview");
        AddMeta(html, "Order form", request.OrderFormReference);
        AddMeta(html, "Pick site", request.Site.PickFromSite + " " + request.Site.PickFromSiteName);
        AddMeta(html, "Point of Use site", request.Site.LocationCode + " " + request.Site.LocationName);
        AddMeta(html, "Order reference", request.OrderReference);
        AddMeta(html, "User", request.User.UserId);
        AddMeta(html, "Date", DateTime.Now.ToString("dd MMM yyyy HH:mm"));
        html.AppendLine("</div></div><div class=\"brand\">NZ Blood<br>Directed Transfer</div></section>");

        html.AppendLine("""
            <table>
            <colgroup>
                <col style="width: 5%">
                <col style="width: 6%">
                <col style="width: 12%">
                <col style="width: 10%">
                <col style="width: 27%">
                <col style="width: 6%">
                <col style="width: 19%">
                <col style="width: 7%">
                <col style="width: 8%">
            </colgroup>
            <thead><tr>
            """);
        foreach (var heading in new[] { "Zone", "Priority", "GP Item Code", "Vendor Item", "Description", "UOM", "UOM Description", "Order Up To", "Qty To Order" })
        {
            html.AppendLine("<th>" + heading + "</th>");
        }
        html.AppendLine("</tr></thead><tbody>");

        foreach (var item in rows)
        {
            html.AppendLine("<tr>");
            html.AppendLine("<td class=\"center\">" + Html(item.Zone) + "</td>");
            html.AppendLine("<td class=\"center\">" + item.Priority.ToString("N0") + "</td>");
            html.AppendLine("<td class=\"code\">" + Html(item.ItemNumber) + "</td>");
            html.AppendLine("<td class=\"code\">" + Html(item.VendorItemNumber) + "</td>");
            html.AppendLine("<td class=\"text\">" + Html(item.ItemDescription) + "</td>");
            html.AppendLine("<td class=\"code\">" + Html(item.UnitOfMeasure) + "</td>");
            html.AppendLine("<td class=\"text\">" + Html(item.UomLongDescription) + "</td>");
            html.AppendLine("<td class=\"center-num\">" + item.OrderUpToLevel.ToString("N0") + "</td>");
            if (request.ReportOption == 1)
            {
                html.AppendLine("<td class=\"num write-in-cell\"><span class=\"write-in\"></span></td>");
            }
            else
            {
                html.AppendLine("<td class=\"num\">" + item.QtyToOrder.ToString("N0") + "</td>");
            }

            html.AppendLine("</tr>");
        }

        html.AppendLine("</tbody></table>");
        html.AppendLine("</body></html>");
        return html.ToString();
    }

    private static void AddMeta(StringBuilder html, string label, string value)
    {
        html.AppendLine("<span>" + Html(label) + "</span><strong>" + Html(value) + "</strong>");
    }

    private static string Html(string? value)
        => WebUtility.HtmlEncode(value ?? string.Empty);
}
