using JetBrains.Annotations;

namespace Sparkitect.Graphing;

/// <summary>Marks a class as a graph-local service, collected into a per-graph container discriminated by <typeparamref name="TGraphBaseType"/> and registered as <typeparamref name="TInterface"/>.</summary>
/// <typeparam name="TInterface">The interface this service is registered as.</typeparam>
/// <typeparam name="TGraphBaseType">The graphing category this service belongs to.</typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[PublicAPI]
[MeansImplicitUse]
public sealed class GraphLocalAttribute<TInterface, TGraphBaseType> : Attribute where TInterface : class;
