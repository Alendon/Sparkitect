using JetBrains.Annotations;

namespace Sparkitect.Graphics.RenderGraph_Deprecated.Resources;

/// <summary>Coarse read intent claimed by a pass against a readable image.</summary>
[PublicAPI]
public enum ReadUsage
{
    TransferSrc,
}
