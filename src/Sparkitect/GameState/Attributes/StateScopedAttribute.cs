using Sparkitect.DI.GeneratorAttributes;

namespace Sparkitect.GameState.Attributes;

[FactoryGenerationType(FactoryGenerationType.Service)]
public class StateScopedAttribute<TInterface> : Attribute, IFactoryMarker<TInterface> where TInterface : class
{
    
}

[FactoryGenerationType(FactoryGenerationType.Service)]
public class StateScopedAttribute<TInterface, TStateExposure> : Attribute, IFactoryMarker<TInterface>, IFactoryMarker<TStateExposure> where TInterface : class where TStateExposure : class
{
    
}