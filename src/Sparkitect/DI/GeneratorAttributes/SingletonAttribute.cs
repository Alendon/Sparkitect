namespace Sparkitect.DI.GeneratorAttributes;

[AttributeUsage(AttributeTargets.Class)]
[FactoryGenerationType(FactoryGenerationType.Service)]
public class SingletonAttribute<TInterface> : FactoryAttribute<TInterface> where TInterface : class;