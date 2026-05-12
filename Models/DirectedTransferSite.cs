namespace NZBlood.DirectedTransfer.Blazor.Models;

public sealed class DirectedTransferSite
{
    public string LocationCode { get; init; } = string.Empty;
    public string LocationName { get; init; } = string.Empty;
    public string PickFromSite { get; init; } = string.Empty;
    public string PickFromSiteName { get; init; } = string.Empty;
    public string SiteTransferEmailAddress { get; init; } = string.Empty;
}
