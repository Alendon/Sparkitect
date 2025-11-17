using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect.DI;

public abstract class Registrations<TRegistry> : IConfigurationEntrypoint<RegistrationsEntrypointAttribute<TRegistry>>
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
}