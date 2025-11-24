using JetBrains.Annotations;
using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect.DI;

/// <summary>
/// Configuration entrypoint for registering registry categories with the DI system.
/// Implementations are typically source-generated (marked [CompilerGenerated]) when registry classes
/// are annotated with [Registry] attribute. Manual implementations are possible but rare.
/// </summary>
public interface IRegistryConfigurator : IConfigurationEntrypoint<RegistryConfiguratorAttribute>
{
    /// <summary>
    /// Configures registry factories to be available for resolution.
    /// Called during engine initialization after mods are loaded.
    /// </summary>
    /// <param name="registryBuilder">The factory container builder for registering registry types.</param>
    void ConfigureRegistries(IFactoryContainerBuilder<IRegistryBase> registryBuilder);
}

/// <summary>
/// Marks a class as a registry configurator entrypoint. Automatically applied by source generators
/// to generated IRegistryConfigurator implementations when [Registry] attribute is used.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[MeansImplicitUse]
public class RegistryConfiguratorAttribute : Attribute;