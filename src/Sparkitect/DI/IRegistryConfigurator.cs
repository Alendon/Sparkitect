using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.DI;

/// <summary>
/// Configuration entrypoint for registering registry categories with the DI system.
/// Implementations are typically source-generated (marked [CompilerGenerated]) when registry classes
/// are annotated with [Registry] attribute. Manual implementations are possible but rare.
/// </summary>
/// <remarks>
/// The <see cref="IFactoryConfigurator{TKey,TBase,TDiscoveryAttribute}.Configure"/> method inherited from
/// <see cref="IFactoryConfigurator{TKey,TBase,TDiscoveryAttribute}"/> writes registry factory entries
/// directly into the aggregate registration map owned by <see cref="IDIService.BuildFactoryContainer{TKey,TBase}"/>.
/// </remarks>
public interface IRegistryConfigurator
    : IFactoryConfigurator<string, IRegistryBase, RegistryConfiguratorAttribute>;

/// <summary>
/// Marks a class as a registry configurator entrypoint. Automatically applied by source generators
/// to generated IRegistryConfigurator implementations when [Registry] attribute is used.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[MeansImplicitUse]
public class RegistryConfiguratorAttribute : Attribute;
