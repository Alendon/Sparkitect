namespace Sparkitect.ECS.Capabilities;

/// <summary>
/// Non-generic marker for capability metadata.
/// Each capability type defines its own concrete metadata shape that describes
/// how the capability is provided by a particular storage.
/// </summary>
public interface ICapabilityMetadata;
