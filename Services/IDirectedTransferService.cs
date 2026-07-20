using NZBlood.DirectedTransfer.Blazor.Models;

namespace NZBlood.DirectedTransfer.Blazor.Services;

public interface IDirectedTransferService
{
    Task<IReadOnlyList<DirectedTransferOrderForm>> GetOrderFormsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DirectedTransferItem>> GetItemsAsync(string orderFormId, string pickFromSiteId, string pouSiteId, CancellationToken cancellationToken = default);
    Task<string> CreateTransferAsync(UserContext user, DirectedTransferSite site, string orderReference, IReadOnlyList<DirectedTransferItem> items, CancellationToken cancellationToken = default);
}
