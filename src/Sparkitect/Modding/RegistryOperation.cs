using JetBrains.Annotations;
using Sundew.DiscriminatedUnions;

namespace Sparkitect.Modding;

/// <summary>
/// Operations that <see cref="RegistryState"/> evaluates for phase legality.
/// </summary>
[DiscriminatedUnion]
[PublicAPI]
public abstract partial record RegistryOperation
{
    /// <summary>Allocate a new identification for a mod+category pair.</summary>
    public sealed partial record Allocate(string ModId, string CategoryId) : RegistryOperation;

    /// <summary>Mutate an existing identification.</summary>
    public sealed partial record Mutate(Identification Target) : RegistryOperation;

    /// <summary>Destroy (tear down) an existing identification.</summary>
    public sealed partial record Destroy(Identification Target) : RegistryOperation;
}
