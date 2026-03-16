using JetBrains.Annotations;

namespace Sparkitect.DI.Resolution;

/// <summary>
/// Metadata record mapping a facade dependency type to its backing service interface type.
/// </summary>
/// <param name="ServiceType">The service interface type (e.g., typeof(IFoo)), NOT the implementation type.</param>
[PublicAPI]
public record FacadeMapping(Type ServiceType);
