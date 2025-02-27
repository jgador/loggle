using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

#if !NET
namespace System.Runtime.CompilerServices
{
    /// <summary>Allows capturing of the expressions passed to a method.</summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    internal sealed class CallerArgumentExpressionAttribute : Attribute
    {
        public CallerArgumentExpressionAttribute(string? parameterName)
        {
            this.ParameterName = parameterName;
        }

        public string? ParameterName { get; }
    }
}
#endif

#if !NET && !NETSTANDARD2_1_OR_GREATER
namespace System.Diagnostics.CodeAnalysis
{
    /// <summary>Specifies that an output is not <see langword="null"/> even if
    /// the corresponding type allows it. Specifies that an input argument was
    /// not <see langword="null"/> when the call returns.</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
    internal sealed class NotNullAttribute : Attribute
    {
    }

    /// <summary>Applied to a method that will never return under any circumstance.</summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    internal sealed class DoesNotReturnAttribute : Attribute { }
}
#endif

#pragma warning disable IDE0161 // Convert to file-scoped namespace
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

        public static void IfNullOrWhitespace(
            string? argument,
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
        }

        [DoesNotReturn]
        private static void Throw(string? paramName) => throw new ArgumentNullException(paramName);
    }
}
