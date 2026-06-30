using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph;

/// <summary>
/// Foundation marker interface for a render-graph pass contract.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IPass"/> is deliberately empty. Setup and Execute method shapes are concerns of the
/// stock pass categories (e.g. <see cref="ComputePass"/>) and any mod-defined pass categories; the
/// foundation does not prescribe them.
/// </para>
/// <para>
/// This interface does NOT extend <see cref="Sparkitect.Modding.IHasIdentification"/>. That contract
/// exposes a <c>static abstract Identification</c> member which cannot be forwarded through an
/// intermediate abstract base class. Each concrete leaf pass type implements
/// <see cref="Sparkitect.Modding.IHasIdentification"/> directly and is registered through the standard
/// registry infrastructure.
/// </para>
/// </remarks>
[PublicAPI]
public interface IPass;
