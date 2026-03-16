using Sparkitect.DI.Container;

namespace Sparkitect.DI.Resolution;

/// <summary>
/// Concrete resolution provider that interprets <see cref="FacadeMapping"/> metadata entries
/// to resolve facade dependencies through the container.
/// </summary>
internal class FacadeResolutionProvider : IResolutionProvider
{
    /// <inheritdoc />
    public bool TryResolve(Type serviceType, ICoreContainer container, List<object> metadataEntries, out object? service)
    {
        foreach (var entry in metadataEntries)
        {
            if (entry is FacadeMapping mapping &&
                container.TryResolve(mapping.ServiceType, out service))
            {
                return true;
            }
        }

        service = null;
        return false;
    }
}
