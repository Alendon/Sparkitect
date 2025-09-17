using System.Collections.Generic;
using System.Collections.Immutable;

namespace Sparkitect.GameState;

public sealed class StateActivationPolicy
{
    private readonly ImmutableArray<string> _include;
    private readonly ImmutableArray<string> _exclude;
    private readonly ImmutableArray<string> _groups;

    public StateActivationPolicy(
        IEnumerable<string>? include = null,
        IEnumerable<string>? exclude = null,
        IEnumerable<string>? groups = null)
    {
        _include = include is null ? ImmutableArray<string>.Empty : ImmutableArray.CreateRange(include);
        _exclude = exclude is null ? ImmutableArray<string>.Empty : ImmutableArray.CreateRange(exclude);
        _groups = groups is null ? ImmutableArray<string>.Empty : ImmutableArray.CreateRange(groups);
    }

    public IReadOnlyList<string> Include => _include;
    public IReadOnlyList<string> Exclude => _exclude;
    public IReadOnlyList<string> Groups => _groups;
}

