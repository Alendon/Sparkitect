using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph_Deprecated;

/// <summary>
/// Foundation marker interface for a render-graph pass contract.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IPass"/> is deliberately empty. Setup, Execute, and lifecycle-hook
/// interactions are emergent-layer concerns implemented by stock pass categories
/// (e.g. <c>ComputePass</c>, Phase 51) and by manually authored mod-defined pass
/// categories. The foundation does not prescribe a setup/execute method shape.
/// </para>
/// <para>
/// This interface does NOT extend <see cref="Sparkitect.Modding.IHasIdentification"/>.
/// <c>IHasIdentification</c> exposes a <c>static abstract Identification</c>
/// member which cannot be forwarded through an intermediate abstract base class.
/// Instead, each concrete leaf pass type implements
/// <see cref="Sparkitect.Modding.IHasIdentification"/> directly and is registered
/// through the standard Sparkitect registry infrastructure
/// (<see cref="Sparkitect.Modding.IRegistry"/>).
/// </para>
/// </remarks>
[PublicAPI]
public interface IPass;
