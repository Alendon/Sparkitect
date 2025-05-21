namespace Sparkitect.DI;

public interface IServiceFactory
{
    Type ServiceType { get; }
    Type ImplementationType { get; }
    
    (Type Type, bool IsOptional)[] GetConstructorDependencies();
    (Type Type, string PropertyName, bool IsOptional)[] GetPropertyDependencies();
    
    object CreateInstance(ICoreContainerBuilder container);
    void ApplyProperties(object instance, ICoreContainerBuilder container);
}