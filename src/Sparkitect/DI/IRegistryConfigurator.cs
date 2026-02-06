using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.DI;

/// <summary>
/// Configuration entrypoint for registering registry categories with the DI system.
/// Implementations are typically source-generated (marked [CompilerGenerated]) when registry classes
/// are annotated with [Registry] attribute. Manual implementations are possible but rare.
/// </summary>
/// <remarks>
/// The <see cref="IFactoryConfigurator{TBase, TDiscoveryAttribute}.Configure"/> method inherited from
/// <see cref="IFactoryConfigurator{TBase, TDiscoveryAttribute}"/> registers keyed registry factories.
/// </remarks>
public interface IRegistryConfigurator : IFactoryConfigurator<IRegistryBase, RegistryConfiguratorAttribute>;

/// <summary>
/// Marks a class as a registry configurator entrypoint. Automatically applied by source generators
/// to generated IRegistryConfigurator implementations when [Registry] attribute is used.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[MeansImplicitUse]
public class RegistryConfiguratorAttribute : Attribute;