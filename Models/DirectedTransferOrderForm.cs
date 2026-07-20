namespace NZBlood.DirectedTransfer.Blazor.Models;

public sealed class DirectedTransferOrderForm
{
    public string OrderFormId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DirectedTransferSite Site { get; init; } = new();

    public string DisplayName => OrderFormId + " : " + Description;
}
