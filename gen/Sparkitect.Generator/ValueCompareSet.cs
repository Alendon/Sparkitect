using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Sparkitect.Generator;

public class ValueCompareSet<T> : HashSet<T>
{
    public ValueCompareSet()
    {
    }

    public ValueCompareSet(IEnumerable<T> collection) : base(collection)
    {
    }

    public ValueCompareSet(IEnumerable<T> collection, IEqualityComparer<T> comparer) : base(collection, comparer)
    {
    }

    public ValueCompareSet(IEqualityComparer<T> comparer) : base(comparer)
    {
    }

    protected ValueCompareSet(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    public override bool Equals(object? obj)
    {
        return obj is ValueCompareSet<T> other && Equals(other);
    }

    public bool Equals(ValueCompareSet<T> other)
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