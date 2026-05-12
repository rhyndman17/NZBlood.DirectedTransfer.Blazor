using NZBlood.DirectedTransfer.Blazor.Models;

namespace NZBlood.DirectedTransfer.Blazor.Services;

public interface IDirectedTransferEmailService
{
    Task<string> SendReportAsync(DirectedTransferReportRequest request, GeneratedReportFile report, CancellationToken cancellationToken = default);
}
