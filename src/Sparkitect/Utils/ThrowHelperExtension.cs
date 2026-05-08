using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Sparkitect.Utils;

public static class ThrowHelperExtension
{
    extension(Exception instance)
    {
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Throw()
        {
            throw instance;
        }
    }
}