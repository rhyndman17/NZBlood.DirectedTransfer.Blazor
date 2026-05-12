using NZBlood.DirectedTransfer.Blazor.Models;

namespace NZBlood.DirectedTransfer.Blazor.Services;

public interface IDirectedTransferReportService
{
    Task<GeneratedReportFile> GenerateAsync(DirectedTransferReportRequest request, CancellationToken cancellationToken = default);
}
