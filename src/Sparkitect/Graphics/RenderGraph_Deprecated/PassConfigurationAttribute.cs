using JetBrains.Annotations;
using Sparkitect.Metadata;

namespace Sparkitect.Graphics.RenderGraph_Deprecated;

/// <summary>
/// Marks a pass class as contributing a <see cref="PassConfiguration"/> metadata entry. Class-level
/// <see cref="Sparkitect.Stateless.OrderAfterAttribute{TOther}"/> /
/// <see cref="Sparkitect.Stateless.OrderBeforeAttribute{TOther}"/> on the same class are harvested by
/// the metadata generator into the carrier constructor's attribute arrays — zero generator work.
/// </summary>
[MetadataCategoryMarker]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[PublicAPI]
[MeansImplicitUse]
public sealed class PassConfigurationAttribute : MetadataAttribute<PassConfiguration>;
