using Sparkitect.DI;

namespace Sparkitect.GameState;

[AttributeUsage(AttributeTargets.Class)]
public sealed class StateServiceMappingEntrypointAttribute : Attribute;

public sealed class StateServiceMappingBuilder
{
    private readonly Dictionary<Type, (Type ServiceType, Type[] FacadeTypes)> _mappings = new();

    public void Add(Type interfaceType, Type serviceType, Type[] facadeTypes)
    {
        _mappings[interfaceType] = (serviceType, facadeTypes);
    }

    public IReadOnlyDictionary<Type, (Type ServiceType, Type[] FacadeTypes)> Build()
    {
        return _mappings;
    }
}

public abstract class StateServiceMapping : IConfigurationEntrypoint<StateServiceMappingEntrypointAttribute>
{
    public abstract void Configure(StateServiceMappingBuilder builder);
}
