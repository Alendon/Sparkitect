using JetBrains.Annotations;

namespace Sparkitect.DI.Resolution;

/// <summary>
/// Marks source-generated metadata entrypoint classes for attribute-based discovery.
/// The generic type parameter identifies which wrapper type the entrypoint provides metadata for,
/// enabling runtime construction via <c>typeof(ResolutionMetadataEntrypointAttribute&lt;&gt;).MakeGenericType(wrapperType)</c>.
/// </summary>
/// <typeparam name="TWrapperType">The wrapper/factory type this entrypoint provides metadata for.</typeparam>
[PublicAPI]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ResolutionMetadataEntrypointAttribute<TWrapperType> : Attribute;
