using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Sparkitect.Utils;

/// <summary>
/// Extension that throws a captured exception from a helper call site while preserving the original
/// stack trace, letting expression-bodied members fail without a statement-level <c>throw</c>.
/// </summary>
[PublicAPI]
public static class ThrowHelperExtension
{
    extension(Exception instance)
    {
        /// <summary>
        /// Throws this exception. Never returns.
        /// </summary>
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Throw()
        {
            throw instance;
        }
    }
}