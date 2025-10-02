using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Sparkitect.Generator;

// Immutable-like value array with value equality (ordered sequence equality).
// Supports collection-initializer syntax via Add during construction.
public class ImmutableValueArray<T> : IReadOnlyCollection<T>, IEquatable<ImmutableValueArray<T>>,
    IStructuralComparable, IStructuralEquatable
{
    public int Count => _values.Length;

    private readonly T[] _values;

    public ImmutableValueArray()
    {
        _values = [];
    }

    //Never call directly, internal for extension methods
    internal ImmutableValueArray(T[] values)
    {
        _values = values;
    }
    
    public T this[int index] => _values[index];

    public IEnumerator<T> GetEnumerator()
    {
        return ((IEnumerable<T>)_values).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public bool Equals(ImmutableValueArray<T> other)
    {
        if (Count != other.Count)
            return false;

        var comparer = EqualityComparer<T>.Default;
        for (int i = 0; i < _values.Length; i++)
        {
            if (!comparer.Equals(_values[i], other._values[i]))
                return false;
        }
        return true;
    }

    public int CompareTo(object? other, IComparer comparer)
    {
        if (other is null) return 1;
        if (other is not ImmutableValueArray<T> o)
            throw new ArgumentException("Object must be ImmutableValueArray<T>", nameof(other));

        // Lexicographic comparison using provided comparer
        int min = Math.Min(Count, o.Count);
        for (int i = 0; i < min; i++)
        {
            int c = comparer.Compare(_values[i], o._values[i]);
            if (c != 0) return c;
        }

        if (Count == o.Count) return 0;
        return Count < o.Count ? -1 : 1;
    }

    public bool Equals(object? other, IEqualityComparer comparer)
    {
        if (other is not ImmutableValueArray<T> o)
            return false;

        if (Count != o.Count)
            return false;

        for (int i = 0; i < _values.Length; i++)
        {
            if (!comparer.Equals(_values[i], o._values[i]))
                return false;
        }
        return true;
    }

    public int GetHashCode(IEqualityComparer comparer)
    {
        unchecked
        {
            int hash = 17;
            foreach (var item in _values)
            {
                hash = hash * 31 + (item is null ? 0 : comparer.GetHashCode(item));
            }
            return hash;
        }
    }

    public sealed class Builder : List<T>
    {
        public ImmutableValueArray<T> ToImmutableValueArray()
        {
            return new ImmutableValueArray<T>(ToArray());
        }
    }
}
