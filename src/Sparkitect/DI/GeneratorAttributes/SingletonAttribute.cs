namespace Sparkitect.DI.GeneratorAttributes;

[AttributeUsage(AttributeTargets.Class)]
public class SingletonAttribute<TInterface> : Attribute;