namespace Sparkitect.ECS.Commands;

/// <summary>
/// Metadata for DI resolution of <see cref="ICommandBufferAccessor"/>.
/// Unlike ComponentQueryMetadata which creates new queries per resolution,
/// this returns the shared singleton accessor instance (D-07, Pitfall 5).
/// </summary>
public class CommandBufferAccessorMetadata
{
    private readonly ICommandBufferAccessor _accessor;

    public CommandBufferAccessorMetadata(ICommandBufferAccessor accessor)
    {
        _accessor = accessor;
    }

    /// <summary>
    /// Returns the shared accessor instance.
    /// </summary>
    public ICommandBufferAccessor GetAccessor() => _accessor;
}
