using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect.DI;


public abstract class Registrations : ConfigurationEntrypoint<RegistrationsEntrypointAttribute>
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

    public abstract void ProcessRegistrations(IRegistry registry);
}

public abstract class Registrations<TRegistry> : Registrations where TRegistry : IRegistry
{
    public sealed override void ProcessRegistrations(IRegistry registry)
    {
        ProcessRegistrations((TRegistry)registry);
    }

    public abstract void ProcessRegistrations(TRegistry registry);
}
