namespace Sparkitect.DI.GeneratorAttributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class PropertyInjectParameterAttribute(string parameterName) : Attribute
{
    
}