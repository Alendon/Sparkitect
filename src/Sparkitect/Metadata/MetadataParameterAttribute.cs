using JetBrains.Annotations;

namespace Sparkitect.Metadata;

/// <summary>Base for harvestable metadata parameter attributes, recognized structurally by inheritance.</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
[PublicAPI]
public abstract class MetadataParameterAttribute : Attribute;
