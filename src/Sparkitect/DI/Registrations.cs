using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect.DI;

/// <summary>
/// Base interface for runtime polymorphic access to registrations
/// </summary>
public interface IRegistrationsBase : IBaseConfigurationEntrypoint
{
    string CategoryIdentifier { get; }
    void Initialize(ICoreContainer container);
    void ProcessRegistrationsUntyped(IRegistry registry);
}

public abstract class Registrations<TRegistry> : IRegistrationsBase, IConfigurationEntrypoint<RegistrationsEntrypointAttribute<TRegistry>>
    where TRegistry : class, IRegistry
{
    public abstract string CategoryIdentifier { get; }

    protected ICoreContainer Container { get; private set; } = null!;
    protected IIdentificationManager IdentificationManager { get; private set; } = null!;
    protected IResourceManager ResourceManager { get; private set; } = null!;

    public void Initialize(ICoreContainer container)
    {
        Container = container;

        IdentificationManager = container.Resolve<IIdentificationManager>();
        ResourceManager = container.Resolve<IResourceManager>();
    }

    public abstract void ProcessRegistrations(TRegistry registry);

    void IRegistrationsBase.ProcessRegistrationsUntyped(IRegistry registry)
    {
        ProcessRegistrations((TRegistry)registry);
    }
}