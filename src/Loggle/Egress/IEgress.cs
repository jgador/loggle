using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;

namespace Loggle.Egress
{
    public interface IEgress
    {
        ValueTask SendLogAsync<TState>(LogEntry<TState> logEntry);
    }
}
