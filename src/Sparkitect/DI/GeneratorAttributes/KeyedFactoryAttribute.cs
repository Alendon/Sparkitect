namespace Sparkitect.DI.GeneratorAttributes;

[AttributeUsage(AttributeTargets.Class)]
public class KeyedFactoryAttribute<TBase> : Attribute where TBase : class
{
    public KeyedFactoryAttribute([Key] string? key = null, [KeyProperty] string? propertyName = null)
    {
    }
}