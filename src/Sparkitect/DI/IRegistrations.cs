namespace Sparkitect.DI;

/// <summary>
/// Interface for classes that define registration logic for objects
/// </summary>
public interface IRegistrations : ConfigurationEntrypoint<RegistrationsEntrypointAttribute>
{
    string CategoryIdentifier { get; }
    
    
    /// <summary>
    /// Executes the main phase registration logic
    /// </summary>
    void MainPhaseRegistration();
}