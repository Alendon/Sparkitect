using JetBrains.Annotations;

namespace Sparkitect.ECS.Capabilities;

/// <summary>
/// Capability metadata declaring that a storage provides entity identity tracking.
/// </summary>
[PublicAPI]
public record EntityIdentityMetadata : ICapabilityMetadata;
