using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect.DI;

/// <summary>
/// Base class for registry registration entrypoints. Implementations are source-generated (marked [CompilerGenerated])
/// when registration attributes (generated from [RegistryMethod]) are used on static methods/properties.
/// </summary>
/// <typeparam name="TRegistry">The registry type that registrations will be processed against.</typeparam>
public abstract class Registrations<TRegistry> : IConfigurationEntrypoint<RegistrationsEntrypointAttribute<TRegistry>>
    where TRegistry : class, IRegistry
{
    /// <summary>
    /// Gets the registry category identifier.
    /// </summary>
    public abstract string CategoryIdentifier { get; }

    /// <summary>
    /// Gets the DI container, available after <see cref="Initialize"/> is called.
    /// </summary>
    protected ICoreContainer Container { get; private set; } = null!;

    /// <summary>
    /// Gets the identification manager, available after <see cref="Initialize"/> is called.
    /// </summary>
    protected IIdentificationManager IdentificationManager { get; private set; } = null!;

    /// <summary>
    /// Gets the resource manager, available after <see cref="Initialize"/> is called.
    /// </summary>
    protected IResourceManager ResourceManager { get; private set; } = null!;

    /// <summary>
    /// Initializes the registration instance with DI-resolved services.
    /// </summary>
    /// <param name="container">The core container to resolve services from.</param>
    public void Initialize(ICoreContainer container)
    {
        Container = container;

        IdentificationManager = container.Resolve<IIdentificationManager>();
        ResourceManager = container.Resolve<IResourceManager>();
    }

    /// <summary>
    /// Processes all discovered registrations for this registry type.
    /// Generated implementations invoke registry methods with data from registration attributes.
    /// </summary>
    /// <param name="registry">The registry instance to register objects into.</param>
    public abstract void ProcessRegistrations(TRegistry registry);
}