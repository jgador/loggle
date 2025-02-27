using System.Threading;
using System.Threading.Tasks;

namespace Loggle.Web.Authentication.ApiKey;

public class AlwaysApprovedApiKeyValidator : IApiKeyValidator
{
    public async Task<bool> IsValidAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        // Always return true for testing purposes
        return await Task.FromResult(true).ConfigureAwait(false);
    }
}
