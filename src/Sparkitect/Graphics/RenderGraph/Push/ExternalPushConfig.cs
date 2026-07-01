using JetBrains.Annotations;
using Sparkitect.Metadata;

namespace Sparkitect.Graphics.RenderGraph.Push;

/// <summary>
/// Metadata payload marking a resource moment as externally-pushed. Carried by the general
/// Identification-mapped metadata mechanism, so the render graph discovers which registered moments are
/// fed through <see cref="IExternalPushHandler"/> and synthesizes their birth increments at setup.
/// </summary>
[PublicAPI]
public sealed class ExternalPushConfig;

/// <summary>
/// Marks an <c>IHasIdentification</c> moment-carrier type as externally-pushed, attaching an
/// <see cref="ExternalPushConfig"/> under its identification via the metadata mechanism
/// (zero generator work — the marker plus payload are the whole category).
/// </summary>
[MetadataCategoryMarker]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[PublicAPI]
[MeansImplicitUse]
public sealed class ExternalPushAttribute : MetadataAttribute<ExternalPushConfig>;
