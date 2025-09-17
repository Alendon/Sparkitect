using JetBrains.Annotations;

namespace Sparkitect.GameState;

[PublicAPI]
public sealed class StateModuleDescriptor
{
    public required string Id { get; init; }
    public required Type ModuleType { get; init; }
}

[PublicAPI]
public sealed class ActivationPolicy
{
    // Keys are public const string defined by modules
    public HashSet<string> Include { get; } = new();
    public HashSet<string> Exclude { get; } = new();
    public HashSet<string> Groups { get; } = new();
}

[PublicAPI]
public sealed class StateDescriptor
{
    public required string Id { get; init; }
    public string? ParentId { get; init; }

    // Ordered modules for this state
    public required IReadOnlyList<StateModuleDescriptor> Modules { get; init; }

    // Optional per-module activation policies (by module id)
    public IReadOnlyDictionary<string, ActivationPolicy>? Activation { get; init; }
}

