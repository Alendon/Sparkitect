using System;
using System.Collections.Generic;
using System.Linq;

namespace Sparkitect.Generator;

public class ValueCompareList<T> : List<T>
{
    public ValueCompareList()
    {
    }

    public ValueCompareList(IEnumerable<T> collection) : base(collection)
    {
    }

    public ValueCompareList(int capacity) : base(capacity)
    {
    }


    public override bool Equals(object? obj)
    {
        return obj is ValueCompareList<T> other && Equals(other);
    }

    public bool Equals(ValueCompareList<T> other)
    {
        return this.SequenceEqual(other);
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();

        foreach (var value in this)
        {
            hashCode.Add(value);
        }
        return hashCode.ToHashCode();
    }
}