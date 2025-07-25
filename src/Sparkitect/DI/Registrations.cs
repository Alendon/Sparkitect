using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect.DI;


public abstract class Registrations : ConfigurationEntrypoint<RegistrationsEntrypointAttribute>
{
    public abstract string CategoryIdentifier { get; }
    
    public virtual void PrePhaseRegistration(IRegistry registry, ICoreContainer container) { }
    public abstract void MainPhaseRegistration(IRegistry registry, ICoreContainer container);
    public virtual void PostPhaseRegistration(IRegistry registry, ICoreContainer container) { }
}

public abstract class Registrations<TRegistry> : Registrations where TRegistry : IRegistry
{
    public sealed override void PrePhaseRegistration(IRegistry registry, ICoreContainer container)
    {
        PrePhaseRegistration((TRegistry) registry, container);
    }

    public sealed override void MainPhaseRegistration(IRegistry registry, ICoreContainer container)
    {
        MainPhaseRegistration((TRegistry) registry, container);
    }

    public sealed override void PostPhaseRegistration(IRegistry registry, ICoreContainer container)
    {
        PostPhaseRegistration((TRegistry) registry, container);
    }

    public abstract void PrePhaseRegistration(TRegistry registry, ICoreContainer container);
    public abstract void MainPhaseRegistration(TRegistry registry, ICoreContainer container);
    public abstract void PostPhaseRegistration(TRegistry registry, ICoreContainer container);
}