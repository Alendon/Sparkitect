using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Foundation marker interface for a render-graph pass contract; deliberately empty. Does NOT extend
/// <see cref="Sparkitect.Modding.IHasIdentification"/> — its <c>static abstract Identification</c> cannot
/// forward through an abstract base, so each concrete leaf pass implements it directly.
/// </summary>
[PublicAPI]
public interface IPass;
