using System.DirectoryServices;
using NZBlood.DirectedTransfer.Blazor.Models;

namespace NZBlood.DirectedTransfer.Blazor.Services;

public sealed class UserContextService : IUserContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UserContextService> _logger;

    public UserContextService(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<UserContextService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _logger = logger;
    }

    public Task<UserContext> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var domainName = _configuration["DirectedTransfer:DomainName"] ?? string.Empty;
        var identityName = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
        var userId = string.IsNullOrWhiteSpace(identityName) ? Environment.UserName : identityName;
        userId = userId.Replace(domainName + @"\", string.Empty, StringComparison.OrdinalIgnoreCase);

        return Task.FromResult(new UserContext
        {
            UserId = userId,
            EmailAddress = GetUserEmail(userId)
        });
    }

    private string GetUserEmail(string userId)
    {
        try
        {
            var accountName = userId.Split('\\').Last().ToLowerInvariant();
            using var searcher = userId.Contains('\\')
                ? new DirectorySearcher("LDAP://" + userId.Split('\\').First().ToLowerInvariant())
                : new DirectorySearcher();

            searcher.Filter = "(&(ObjectClass=person)(sAMAccountName=" + accountName + "))";

            var result = searcher.FindOne();
            return result?.Properties["mail"]?.Count > 0
                ? Convert.ToString(result.Properties["mail"][0]) ?? string.Empty
                : string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to resolve email address for {UserId}.", userId);
            return string.Empty;
        }
    }
}
