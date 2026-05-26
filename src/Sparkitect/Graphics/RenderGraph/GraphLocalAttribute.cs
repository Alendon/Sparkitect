using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Marks a class as a per-render-graph-instance service. The class is resolved from
/// the render graph's child container and lives for the lifetime of that container.
/// </summary>
/// <typeparam name="TInterface">The interface this service implements; the key used in the
/// per-graph child container's registration map.</typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[PublicAPI]
[MeansImplicitUse]
public sealed class GraphLocalAttribute<TInterface> : Attribute where TInterface : class;
