namespace Sparkitect.DI;

/// <summary>
/// Configuration entrypoint for registering facade mappings with the DI system.
/// Implementations are source-generated (marked [CompilerGenerated]) for each facade marker attribute type.
/// Facades enable subsystem-exclusive APIs not resolvable through the main container.
/// </summary>
/// <typeparam name="TMarkerAttribute">The facade marker attribute type identifying which facades this configurator handles.</typeparam>
public interface IFacadeConfigurator<TMarkerAttribute> : IConfigurationEntrypoint<FacadeConfiguratorEntrypointAttribute<TMarkerAttribute>>
    where TMarkerAttribute : Attribute
{
    /// <summary>
    /// Configures facade mappings for the associated marker attribute type.
    /// Called during container construction to establish subsystem-exclusive interface mappings.
    /// </summary>
    /// <param name="facadeHolder">The holder for registering facade type mappings.</param>
    public void ConfigureFacades(IFacadeHolder facadeHolder);
}

/// <summary>
/// Marks source-generated facade configurator implementations. Applied automatically by generators
/// when facade marker attributes are detected.
/// </summary>
/// <typeparam name="TFacadeMarkerAttribute">The facade marker attribute type.</typeparam>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class FacadeConfiguratorEntrypointAttribute<TFacadeMarkerAttribute> : Attribute where TFacadeMarkerAttribute : Attribute
{
}

/// <summary>
/// Manages facade type mappings during container construction.
/// Facades provide subsystem-exclusive APIs (e.g., registry-to-manager interactions) that bypass
/// normal DI resolution and are only accessible within specific contexts.
/// </summary>
public interface IFacadeHolder
{
    /// <summary>
    /// Registers a facade mapping for subsystem-exclusive resolution.
    /// </summary>
    /// <param name="facadeType">The exclusive facade interface type accessible only in specific contexts.</param>
    /// <param name="serviceType">The public service interface type normally resolvable through DI.</param>
    public void AddFacade(Type facadeType, Type serviceType);
}

internal class FacadeHolder : IFacadeHolder
{
    private Dictionary<Type, Type> _facadeMapping = [];


    public void AddFacade(Type facadeType, Type serviceType)
    {
        _facadeMapping.Add(facadeType, serviceType);
    }

    public IReadOnlyDictionary<Type, Type> GetFacadeMapping()
    {
        return _facadeMapping;
    }
}