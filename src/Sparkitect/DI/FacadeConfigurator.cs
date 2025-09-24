namespace Sparkitect.DI;


public interface IFacadeConfigurator
{
    public void ConfigureFacades(IFacadeHolder facadeHolder);
}

public interface IFacadeHolder
{
    public void AddFacade(Type facadeType, Type serviceType);
}

internal class FacadeHolder
{
    private Dictionary<Type, Type> _facadeMapping = [];
    
    
    public void AddFacade(Type facadeType, Type serviceType)
    {
        _facadeMapping.Add(facadeType, serviceType);;
    }
    
    public IReadOnlyDictionary<Type, Type> GetFacadeMapping()
    {
        return _facadeMapping;
    }
}