using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator;

public static class Extensions
{
    public static IncrementalValuesProvider<T> NotNull<T>(this IncrementalValuesProvider<T?> provider) where T : class
    {
        return provider.Where(x => x is not null).Select((x, _) => x!);
    }

    
}
