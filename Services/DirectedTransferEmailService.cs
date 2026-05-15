using System.Net;
using System.Net.Mail;
using NZBlood.DirectedTransfer.Blazor.Models;

namespace NZBlood.DirectedTransfer.Blazor.Services;

public sealed class DirectedTransferEmailService : IDirectedTransferEmailService
{
    private readonly IConfiguration _configuration;

    public DirectedTransferEmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> SendReportAsync(DirectedTransferReportRequest request, GeneratedReportFile report, CancellationToken cancellationToken = default)
    {
        if (!IsEmailEnabled())
        {
            return "Email sending is disabled, so the PDF was generated but not emailed.";
        }

        var recipients = SplitRecipients(request.Site.SiteTransferEmailAddress);
        if (recipients.Count == 0)
        {
            return "No site transfer email address is configured for this POU site.";
        }

        var host = _configuration["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            return "SMTP host is not configured, so the PDF was generated but not emailed.";
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_configuration["Smtp:From"] ?? "directedtransfer@nzblood.co.nz"),
            Subject = "Directed Transfer " + (request.TransferOrderCode ?? "Report"),
            Body = "Please find the directed transfer report attached.",
            IsBodyHtml = false
        };

        foreach (var recipient in recipients)
        {
            message.To.Add(recipient);
        }

        message.Attachments.Add(new Attachment(new MemoryStream(report.Content), report.FileName, "application/pdf"));

        using var client = new SmtpClient(host, _configuration.GetValue<int?>("Smtp:Port") ?? 25)
        {
            EnableSsl = _configuration.GetValue<bool>("Smtp:EnableSsl")
        };

        var userName = _configuration["Smtp:UserName"];
        var password = _configuration["Smtp:Password"];
        if (!string.IsNullOrWhiteSpace(userName))
        {
            client.Credentials = new NetworkCredential(userName, password);
        }

        await client.SendMailAsync(message, cancellationToken);
        return "PDF emailed to " + string.Join(", ", recipients) + ".";
    }

    private static IReadOnlyList<string> SplitRecipients(string value)
        => value.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private bool IsEmailEnabled()
    {
        var value = _configuration["Smtp:SendEmail"];
        return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
    }
}
