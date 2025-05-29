namespace Sparkitect.DI.GeneratorAttributes;

[AttributeUsage(AttributeTargets.Class)]
public class EntrypointFactoryAttribute<TBase> : Attribute where TBase : class
{
    
}