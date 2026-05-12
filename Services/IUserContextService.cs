using NZBlood.DirectedTransfer.Blazor.Models;

namespace NZBlood.DirectedTransfer.Blazor.Services;

public interface IUserContextService
{
    Task<UserContext> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}
