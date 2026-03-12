namespace Sparkitect.ECS.Capabilities;

/// <summary>
/// Abstract base for capability registrations. Each registration carries typed metadata
/// and supports polymorphic matching against <see cref="ICapabilityRequirement"/> instances
/// without reflection.
/// </summary>
public abstract class CapabilityRegistration
{
    /// <summary>
    /// Attempts to match this registration against the given requirement using CLR pattern matching.
    /// Returns <c>true</c> if the requirement is a typed <see cref="ICapabilityRequirement{TMeta}"/>
    /// whose metadata type matches and whose <see cref="ICapabilityRequirement{TMeta}.Matches"/> returns <c>true</c>.
    /// </summary>
    /// <param name="requirement">The capability requirement to match against.</param>
    /// <returns><c>true</c> if this registration satisfies the requirement; otherwise <c>false</c>.</returns>
    internal abstract bool TryMatch(ICapabilityRequirement requirement);
}

/// <summary>
/// Sealed generic subclass carrying typed metadata for a specific <typeparamref name="TMeta"/> type.
/// The CLR dispatches <see cref="TryMatch"/> via virtual call, avoiding all reflection.
/// </summary>
/// <typeparam name="TMeta">The capability metadata type this registration carries.</typeparam>
public sealed class CapabilityRegistration<TMeta> : CapabilityRegistration
    where TMeta : ICapabilityMetadata
{
    private readonly TMeta _metadata;

    /// <summary>
    /// Creates a new capability registration with the specified metadata.
    /// </summary>
    /// <param name="metadata">The metadata describing how the capability is provided.</param>
    public CapabilityRegistration(TMeta metadata)
    {
        _metadata = metadata;
    }

    /// <inheritdoc/>
    internal override bool TryMatch(ICapabilityRequirement requirement)
    {
        if (requirement is ICapabilityRequirement<TMeta> typed)
            return typed.Matches(_metadata);

        return false;
    }
}
