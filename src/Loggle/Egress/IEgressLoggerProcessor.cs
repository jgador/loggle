using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Loggle.Logging;

namespace Loggle.Egress
{
    public interface IEgressLoggerProcessor
    {
        Task EgressAsync(IReadOnlyList<LogMessageEntry> batch, CancellationToken cancellationToken);
    }
}
