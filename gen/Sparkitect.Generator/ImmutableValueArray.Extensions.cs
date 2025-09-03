using System;
using System.Collections.Generic;
using System.Linq;

namespace Sparkitect.Generator;

public static class ImmutableValueArray
{
    public static ImmutableValueArray<T> ToImmutableValueArray<T>(this IEnumerable<T> values)
    {
        return new ImmutableValueArray<T>(values.ToArray());
    }

    public static ImmutableValueArray<T> ToImmutableValueArray<T>(this Span<T> values)
    {
        return new ImmutableValueArray<T>(values.ToArray());
    }
    
    public static ImmutableValueArray<T> ToImmutableValueArray<T>(this T[] values)
    {
        return new ImmutableValueArray<T>(values.ToArray());
    }
    
    public static ImmutableValueArray<T> ToImmutableValueArray<T>(this IList<T> values)
    {
        return new ImmutableValueArray<T>(values.ToArray());
    }

    public static ImmutableValueArray<T> From<T>(params Span<T> values)
    {
        return new ImmutableValueArray<T>(values.ToArray());
    }
}