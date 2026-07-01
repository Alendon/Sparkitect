using JetBrains.Annotations;
using Sparkitect.DI.Container;

namespace Sparkitect.Graphing;

/// <summary>
/// Non-generic base for source-generated graph-local service configurators. The runtime collects
/// implementations via a closed <see cref="GraphLocalServiceEntryAttribute{TGraphBaseType}"/> marker and
/// invokes <see cref="Configure"/> to populate a per-graph core container.
/// </summary>
[PublicAPI]
public interface IGraphLocalConfigurator
{
    /// <summary>
    /// Registers graph-local services with the container builder.
    /// </summary>
    void Configure(ICoreContainerBuilder builder, IReadOnlySet<string> loadedMods);
}
