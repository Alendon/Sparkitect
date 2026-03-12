namespace Sparkitect.ECS.Capabilities;

/// <summary>
/// Pairs a capability CLR type with its metadata for storage registration.
/// Passed to <see cref="World"/> when adding a storage to declare which capabilities it provides.
/// </summary>
public readonly struct CapabilityRegistration
{
    /// <summary>
    /// The closed generic capability interface type (e.g., <c>typeof(IPositionCapability&lt;MyKey&gt;)</c>).
    /// </summary>
    public required Type CapabilityType { get; init; }

    /// <summary>
    /// Metadata describing how this capability is provided by the registering storage.
    /// </summary>
    public required ICapabilityMetadata Metadata { get; init; }
}
