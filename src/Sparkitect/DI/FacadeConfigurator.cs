namespace Sparkitect.DI;


public interface IFacadeConfigurator : IConfigurationEntrypoint<FacadeConfiguratorEntrypointAttribute>
{
    public void ConfigureFacades(IFacadeHolder facadeHolder);
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class FacadeConfiguratorEntrypointAttribute : Attribute
{
}

public interface IFacadeHolder
{
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