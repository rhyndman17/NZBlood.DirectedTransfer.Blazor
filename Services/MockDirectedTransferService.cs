using NZBlood.DirectedTransfer.Blazor.Models;

namespace NZBlood.DirectedTransfer.Blazor.Services;

public sealed class MockDirectedTransferService : IDirectedTransferService
{
    public Task<IReadOnlyList<DirectedTransferOrderForm>> GetOrderFormsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DirectedTransferOrderForm> forms =
        [
            new()
            {
                OrderFormId = "AKLD-AKLG",
                Description = "Auckland Hospital Consumables",
                Site = new() { LocationCode = "AKLD", LocationName = "Auckland Hospital POU", PickFromSite = "AKLG", PickFromSiteName = "Auckland Logistics", SiteTransferEmailAddress = "site.transfer@example.org" }
            },
            new()
            {
                OrderFormId = "WGTN-WGLG",
                Description = "Wellington Hospital Consumables",
                Site = new() { LocationCode = "WGTN", LocationName = "Wellington Hospital POU", PickFromSite = "WGLG", PickFromSiteName = "Wellington Logistics", SiteTransferEmailAddress = "wellington.transfer@example.org" }
            }
        ];

        return Task.FromResult(forms);
    }

    public Task<IReadOnlyList<DirectedTransferItem>> GetItemsAsync(string orderFormId, string pickFromSiteId, string pouSiteId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DirectedTransferItem> items =
        [
            new()
            {
                Zone = "A",
                Priority = 1,
                ItemNumber = "BAG-CPDA-450",
                ItemDescription = "Blood collection bag CPDA 450ml",
                UnitOfMeasure = "Each",
                QtyBaseUom = 1,
                UomLongDescription = "Each",
                UomSchedule = "EACH",
                BaseUom = "Each",
                QtyPending = 4,
                QtyAvailable = 82,
                OrderUpToLevel = 120,
                VendorItemNumber = "VN-450"
            },
            new()
            {
                Zone = "A",
                Priority = 2,
                ItemNumber = "TUBE-EDTA-10",
                ItemDescription = "EDTA tube 10ml lavender top",
                UnitOfMeasure = "Box",
                QtyBaseUom = 100,
                UomLongDescription = "Box of 100",
                UomSchedule = "BOX",
                BaseUom = "Each",
                QtyPending = 0,
                QtyAvailable = 18,
                OrderUpToLevel = 40,
                VendorItemNumber = "VN-EDTA10"
            },
            new()
            {
                Zone = "B",
                Priority = 3,
                ItemNumber = "KIT-XMATCH",
                ItemDescription = "Crossmatch consumables kit",
                UnitOfMeasure = "Kit",
                QtyBaseUom = 1,
                UomLongDescription = "Kit",
                UomSchedule = "KIT",
                BaseUom = "Kit",
                QtyPending = 6,
                QtyAvailable = 14,
                OrderUpToLevel = 30,
                VendorItemNumber = "VN-XMATCH"
            }
        ];

        return Task.FromResult<IReadOnlyList<DirectedTransferItem>>(items
            .OrderBy(item => item.Zone, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Priority <= 0 ? int.MaxValue : item.Priority)
            .ThenBy(item => item.ItemNumber, StringComparer.OrdinalIgnoreCase)
            .ToList());
    }

    public Task<string> CreateTransferAsync(UserContext user, DirectedTransferSite site, string orderReference, IReadOnlyList<DirectedTransferItem> items, CancellationToken cancellationToken = default)
        => Task.FromResult("XFRMOCK01");
}
