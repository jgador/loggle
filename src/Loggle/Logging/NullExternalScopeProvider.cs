using System;
using Microsoft.Extensions.Logging;

namespace Loggle.Logging
{
    internal sealed class NullExternalScopeProvider : IExternalScopeProvider
    {
        public static IExternalScopeProvider Instance { get; } = new NullExternalScopeProvider();

        void IExternalScopeProvider.ForEachScope<TState>(Action<object?, TState> callback, TState state)
        {
        }

        IDisposable IExternalScopeProvider.Push(object? state)
        {
            return NullScope.Instance;
        }
    }

}
