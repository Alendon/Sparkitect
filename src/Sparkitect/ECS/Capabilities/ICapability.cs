namespace Sparkitect.ECS.Capabilities;

/// <summary>
/// Non-generic marker for all capability interfaces.
/// Capabilities are identified by their closed generic CLR interface type.
/// Concrete storage types implement capability interfaces alongside <see cref="Storage.IStorage"/>
/// to expose typed data access.
/// </summary>
public interface ICapability;
