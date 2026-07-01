using JetBrains.Annotations;

namespace Sparkitect.Graphing;

/// <summary>
/// Discovery marker for source-generated graph-local configurator entrypoints, discriminated by the
/// graphing category <typeparamref name="TGraphBaseType"/>. The runtime collects matching configurators
/// via the closed generic attribute type.
/// </summary>
/// <typeparam name="TGraphBaseType">The graphing category whose configurators this marks.</typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[PublicAPI]
[MeansImplicitUse]
public sealed class GraphLocalServiceEntryAttribute<TGraphBaseType> : Attribute;
