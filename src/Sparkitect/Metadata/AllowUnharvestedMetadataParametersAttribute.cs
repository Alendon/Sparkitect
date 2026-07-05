using JetBrains.Annotations;

namespace Sparkitect.Metadata;

/// <summary>Marks a type as intentionally exempt from metadata parameter-placement analysis.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
[PublicAPI]
public sealed class AllowUnharvestedMetadataParametersAttribute : Attribute;
