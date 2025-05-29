using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator;

public static class Extensions
{
    public static IncrementalValuesProvider<T> NotNull<T>(this IncrementalValuesProvider<T?> provider) where T : class
    {
        return provider.Where(x => x is not null).Select((x, _) => x!);
    }

    public static ValueCompareList<T> ToValueCompareList<T>(this IEnumerable<T> values)
    {
        return new ValueCompareList<T>(values);
    }
}