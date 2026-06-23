using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph_Deprecated.Resources;

/// <summary>Coarse write intent claimed by a pass against a writeable image.</summary>
[PublicAPI]
public enum WriteUsage
{
    TransferDst,
    ComputeStorage,
    ColorAttachment,
}
