namespace Sparkitect.ECS.Systems;

/// <summary>
/// Marker metadata for <see cref="FrameTimingHolder"/> DI resolution.
/// Used by <see cref="EcsResolutionProvider.TryResolve"/> to identify
/// that the requested service should resolve to the cached holder instance.
/// Unlike <see cref="Commands.CommandBufferAccessorMetadata"/> which holds an accessor
/// reference, this is purely a type marker -- the holder is stored on the provider directly.
/// </summary>
public class FrameTimingMetadata;
