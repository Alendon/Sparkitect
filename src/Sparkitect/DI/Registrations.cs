using JetBrains.Annotations;
using Sparkitect.DI.Resolution;
using Sparkitect.Modding;

namespace Sparkitect.DI;

/// <summary>
/// Base class for registry registration entrypoints. Implementations are source-generated (marked [CompilerGenerated])
/// when registration attributes (generated from [RegistryMethod]) are used on static methods/properties.
/// </summary>
/// <typeparam name="TRegistry">The registry type that registrations will be processed against.</typeparam>
[PublicAPI]
public abstract class Registrations<TRegistry> : IConfigurationEntrypoint<RegistrationsEntrypointAttribute<TRegistry>>
    where TRegistry : class, IRegistry
{
    /// <summary>
    /// Gets the registry category identifier.
    /// </summary>
    public abstract string CategoryIdentifier { get; }

    /// <summary>
    /// Gets the resolution scope dependencies are resolved against (used by value-providing
    /// registration methods that declare DI parameters), available after <see cref="Initialize"/> is called.
    /// </summary>
    protected IResolutionScope Scope { get; private set; } = null!;

    /// <summary>
    /// Gets the identification manager, available after <see cref="Initialize"/> is called.
    /// </summary>
    protected IIdentificationManager IdentificationManager { get; private set; } = null!;

    /// <summary>
    /// Gets the resource manager, available after <see cref="Initialize"/> is called.
    /// </summary>
    protected IResourceManager ResourceManager { get; private set; } = null!;

    /// <summary>
    /// Initializes the registration instance with the resolution scope DI-providing
    /// registration methods resolve their parameters against.
    /// </summary>
    /// <param name="scope">The resolution scope to resolve services from.</param>
    public void Initialize(IResolutionScope scope)
    {
        Scope = scope;

        if (!scope.TryResolve<IIdentificationManager>(typeof(Registrations<TRegistry>), out var identificationManager))
            throw new global::System.InvalidOperationException(
                $"Failed to resolve {nameof(IIdentificationManager)} for registrations of {typeof(TRegistry).Name}");
        IdentificationManager = identificationManager;

        if (!scope.TryResolve<IResourceManager>(typeof(Registrations<TRegistry>), out var resourceManager))
            throw new global::System.InvalidOperationException(
                $"Failed to resolve {nameof(IResourceManager)} for registrations of {typeof(TRegistry).Name}");
        ResourceManager = resourceManager;
    }

    /// <summary>
    /// Processes all discovered registrations for this registry type.
    /// Generated implementations invoke registry methods with data from registration attributes.
    /// </summary>
    /// <param name="registry">The registry instance to register objects into.</param>
    public abstract void ProcessRegistrations(TRegistry registry);

    /// <summary>
    /// Processes all teardown unregistrations for this registry type.
    /// Generated implementations call per-entry Unregister methods that mirror the build-up
    /// Register methods — RemoveResource, Unregister, UnregisterObject, and zero the backing field.
    /// Default implementation is a no-op; source-generated classes override to dispatch teardown.
    /// </summary>
    /// <param name="registry">The registry instance to unregister objects from.</param>
    public virtual void ProcessUnregistrations(TRegistry registry) { }
}