namespace Sparkitect.DI.GeneratorAttributes;

[AttributeUsage(AttributeTargets.Class)]
public class ServiceFactoryAttribute<TService> : Attribute where TService : class
{
    
}