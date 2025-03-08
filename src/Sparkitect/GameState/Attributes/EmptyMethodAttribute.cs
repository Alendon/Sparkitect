using JetBrains.Annotations;

namespace Sparkitect.GameState.Attributes;

//TODO Add Analyzer that automatically recognizes empty methods and suggests to use the attribute
//TODO Future Add Codefix

[AttributeUsage(AttributeTargets.Method)]
[PublicAPI]
public class EmptyMethodAttribute : Attribute
{
    
}