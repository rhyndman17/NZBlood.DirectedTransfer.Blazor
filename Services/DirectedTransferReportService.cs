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
        var content = ConvertHtmlToPdf(html);
        var fileName = string.IsNullOrWhiteSpace(request.TransferOrderCode)
            ? request.User.UserId + "_DTPick.pdf"
            : request.TransferOrderCode + ".pdf";
        var report = new GeneratedReportFile(Guid.NewGuid().ToString("N"), fileName, content);
        _cache.Set(report.ReportId, report, TimeSpan.FromMinutes(30));
        return Task.FromResult(report);
    }

    private byte[] ConvertHtmlToPdf(string html)
    {
        try
        {
            var converter = new HtmlToPdfConverter(HtmlRenderingEngine.Blink);
            var settings = new BlinkConverterSettings
            {
                Margin = new PdfMargins { All = 24 },
                Orientation = PdfPageOrientation.Landscape,
                PdfPageSize = PdfPageSize.A4
            };
            settings.CommandLineArguments.Add("--no-sandbox");
            converter.ConverterSettings = settings;

            using var document = converter.Convert(html, string.Empty);
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
            body { color: #201f1e; font-family: 'Segoe UI', Arial, sans-serif; font-size: 12px; margin: 0; }
            .header { align-items: start; border-bottom: 2px solid #a4262c; display: flex; justify-content: space-between; padding-bottom: 12px; }
            h1 { font-size: 22px; margin: 0 0 6px; }
            .meta { display: grid; gap: 4px; grid-template-columns: 120px 1fr; margin-top: 10px; }
            .meta span:nth-child(odd) { color: #605e5c; font-weight: 700; text-transform: uppercase; }
            .brand { color: #a4262c; font-size: 18px; font-weight: 800; text-align: right; }
            table { border-collapse: collapse; margin-top: 16px; width: 100%; }
            th { background: #faf9f8; border-bottom: 1px solid #c8c6c4; color: #323130; font-size: 11px; padding: 8px; text-align: left; text-transform: uppercase; }
            td { border-bottom: 1px solid #edebe9; padding: 7px 8px; vertical-align: top; }
            .num { text-align: right; white-space: nowrap; }
            .pending { color: #a4262c; font-weight: 700; }
            .footer { color: #605e5c; font-size: 10px; margin-top: 16px; }
            </style>
            </head>
            <body>
            """);

        html.AppendLine("<section class=\"header\">");
        html.AppendLine("<div>");
        html.AppendLine("<h1>" + Html(title) + "</h1>");
        html.AppendLine("<div class=\"meta\">");
        AddMeta(html, "Transfer", request.TransferOrderCode ?? "Preview");
        AddMeta(html, "Pick site", request.Site.PickFromSite + " " + request.Site.PickFromSiteName);
        AddMeta(html, "POU site", request.Site.LocationCode + " " + request.Site.LocationName);
        AddMeta(html, "Reference", request.OrderReference);
        AddMeta(html, "User", request.User.UserId);
        AddMeta(html, "Date", DateTime.Now.ToString("dd MMM yyyy HH:mm"));
        html.AppendLine("</div></div><div class=\"brand\">NZ Blood<br>Directed Transfer</div></section>");

        html.AppendLine("<table><thead><tr>");
        foreach (var heading in new[] { "Priority", "GP Item Code", "Vendor Item", "Description", "UOM", "UOM Description", "Qty Pending", "Qty Available", "Order Up To", "Qty To Order" })
        {
            html.AppendLine("<th>" + heading + "</th>");
        }
        html.AppendLine("</tr></thead><tbody>");

        foreach (var item in rows)
        {
            html.AppendLine("<tr>");
            html.AppendLine("<td class=\"num\">" + item.Priority.ToString("N0") + "</td>");
            html.AppendLine("<td>" + Html(item.ItemNumber) + "</td>");
            html.AppendLine("<td>" + Html(item.VendorItemNumber) + "</td>");
            html.AppendLine("<td>" + Html(item.ItemDescription) + "</td>");
            html.AppendLine("<td>" + Html(item.UnitOfMeasure) + "</td>");
            html.AppendLine("<td>" + Html(item.UomLongDescription) + "</td>");
            html.AppendLine("<td class=\"num pending\">" + item.QtyPending.ToString("N0") + "</td>");
            html.AppendLine("<td class=\"num\">" + item.QtyAvailable.ToString("N0") + "</td>");
            html.AppendLine("<td class=\"num\">" + item.OrderUpToLevel.ToString("N0") + "</td>");
            html.AppendLine("<td class=\"num\">" + item.QtyToOrder.ToString("N0") + "</td>");
            html.AppendLine("</tr>");
        }

        html.AppendLine("</tbody></table>");
        html.AppendLine("<p class=\"footer\">Qty Pending includes outstanding Panatracker transfers from all locations.</p>");
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
