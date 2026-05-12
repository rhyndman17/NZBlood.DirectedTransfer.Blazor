namespace NZBlood.DirectedTransfer.Blazor.Models;

public sealed class DirectedTransferReportRequest
{
    public required UserContext User { get; init; }
    public required DirectedTransferSite Site { get; init; }
    public required IReadOnlyList<DirectedTransferItem> Items { get; init; }
    public string OrderReference { get; init; } = string.Empty;
    public int ReportOption { get; init; }
    public string? TransferOrderCode { get; init; }
}
