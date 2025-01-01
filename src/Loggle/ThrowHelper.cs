using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Loggle
{
    internal static class ThrowHelper
    {
        internal static void ThrowIfNull(
            [NotNull] object? argument,
            [CallerArgumentExpression(nameof(argument))] string? paramName = null)
        {
            if (argument is null)
            {
                Throw(paramName);
            }
        }

        [return: NotNull]
        public static string IfNullOrWhitespace(
            [NotNull] string? argument,
            [CallerArgumentExpression(nameof(argument))] string paramName = "")
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                if (argument == null)
                {
                    throw new ArgumentNullException(paramName);
                }
                else
                {
                    throw new ArgumentException("Argument is whitespace", paramName);
                }
            }

            return argument;
        }

        [DoesNotReturn]
        private static void Throw(string? paramName) => throw new ArgumentNullException(paramName);
    }
}
