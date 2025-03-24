using Sparkitect.Modding;

namespace Sparkitect.DI;


public abstract class Registrations : ConfigurationEntrypoint<RegistrationsEntrypointAttribute>
{
    public abstract string CategoryIdentifier { get; }
    
    public abstract void MainPhaseRegistration(IRegistry registry);
}

public abstract class Registrations<TRegistry> : Registrations where TRegistry : IRegistry
{
    public sealed override void MainPhaseRegistration(IRegistry registry)
    {
        MainPhaseRegistration((TRegistry) registry);
    }

    public abstract void MainPhaseRegistration(TRegistry registry);
}