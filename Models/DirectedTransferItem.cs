namespace NZBlood.DirectedTransfer.Blazor.Models;

public sealed class DirectedTransferItem
{
    public string Zone { get; init; } = string.Empty;
    public int Priority { get; init; }
    public string ItemNumber { get; init; } = string.Empty;
    public string ItemDescription { get; init; } = string.Empty;
    public string UnitOfMeasure { get; init; } = string.Empty;
    public int QtyBaseUom { get; init; }
    public string UomLongDescription { get; init; } = string.Empty;
    public string UomSchedule { get; init; } = string.Empty;
    public string BaseUom { get; init; } = string.Empty;
    public int QtyPending { get; init; }
    public int QtyAvailable { get; init; }
    public int OrderUpToLevel { get; init; }
    public int QtyToOrder { get; set; }
    public string VendorItemNumber { get; set; } = string.Empty;
    public string ValidationMessage { get; set; } = string.Empty;
}
