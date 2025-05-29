using JetBrains.Annotations;

namespace Sparkitect.DI.GeneratorAttributes;

public enum FactoryGenerationType
{
    Service,
    Factory,
    Entrypoint
}

public class FactoryGenerationTypeAttribute(FactoryGenerationType generationType) : Attribute;

[AttributeUsage(AttributeTargets.Class)]
public abstract class FactoryAttribute<TExposedType> : Attribute where TExposedType : class;