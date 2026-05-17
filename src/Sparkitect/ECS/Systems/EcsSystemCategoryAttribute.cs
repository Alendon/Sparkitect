using JetBrains.Annotations;

namespace Sparkitect.ECS.Systems;

/// <summary>
/// Marker attribute placed on SF category attribute class definitions to indicate
/// the SF category is an ECS system type. Gates all ECS-specific SG pipelines
/// (resolution metadata, resource access metadata).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[PublicAPI]
public sealed class EcsSystemCategoryAttribute : Attribute;
