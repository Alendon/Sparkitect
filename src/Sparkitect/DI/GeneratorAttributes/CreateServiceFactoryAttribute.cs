namespace Sparkitect.DI.GeneratorAttributes;

[AttributeUsage(AttributeTargets.Class)]
[FactoryGenerationType(FactoryGenerationType.Service)]
public class CreateServiceFactoryAttribute<TInterface> : FactoryAttribute<TInterface> where TInterface : class;