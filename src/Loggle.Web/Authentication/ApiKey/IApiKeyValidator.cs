using System.Threading;
using System.Threading.Tasks;

namespace Loggle.Web.Authentication.ApiKey;

public interface IApiKeyValidator
{
    Task<bool> IsValidAsync(string apiKey, CancellationToken cancellationToken = default);
}
