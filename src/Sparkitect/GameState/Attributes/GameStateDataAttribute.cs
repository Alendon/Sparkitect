using JetBrains.Annotations;

namespace Sparkitect.GameState.Attributes;

//TODO Add Analyzer to check that this attribute is only applied to properties which are classes that implement IGameStateData
//TODO Add Analyzer to check that this attribute is only used inside classes that implement IGameState

[PublicAPI]
[AttributeUsage(AttributeTargets.Property)]
public class GameStateDataAttribute : Attribute
{

}