using JetBrains.Annotations;
using Sparkitect.DI;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Discovery contract for source-generated per-class contributors that populate the
/// per-render-graph child container's factory map. Each generated entry maps a service
/// interface type to its concrete factory type.
/// </summary>
[PublicAPI]
public interface IGraphLocalServiceEntry : IConfigurationEntrypoint<GraphLocalServiceEntryAttribute>
{
    /// <summary>The service interface key (TInterface from [GraphLocal&lt;TInterface&gt;]).</summary>
    Type ServiceInterface { get; }

    /// <summary>The factory Type emitted by GraphLocalServiceGenerator for the impl class.</summary>
    Type FactoryType { get; }
}

/// <summary>
/// Discovery marker for <see cref="IGraphLocalServiceEntry"/> implementations emitted by
/// the GraphLocal service generator.
/// </summary>
[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GraphLocalServiceEntryAttribute : Attribute;
