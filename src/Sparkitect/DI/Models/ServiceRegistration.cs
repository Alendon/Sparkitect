namespace Sparkitect.DI.Models;

internal class ServiceRegistration
{
    public Type ServiceType { get; }
    public Type ImplementationType { get; }
    public object? Instance { get; private set; }
    
    public Type[] ConstructorDependencies { get; set; } = [];
    public List<PropertyDependency> PropertyDependencies { get; } = [];
    
    public bool HasInstance => Instance is not null;
    
    public ServiceRegistration(Type serviceType, Type implementationType)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
    }
    
    public void SetInstance(object instance)
    {
        if (!ImplementationType.IsInstanceOfType(instance))
            throw new InvalidOperationException($"Instance type {instance.GetType()} is not assignable to {ImplementationType}");
        
        Instance = instance;
    }
}