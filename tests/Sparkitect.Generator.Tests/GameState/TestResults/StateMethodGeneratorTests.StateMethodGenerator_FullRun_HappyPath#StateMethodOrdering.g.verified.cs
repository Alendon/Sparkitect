//HintName: StateMethodOrdering.g.cs
namespace GameStateTest;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.GameState.IStateMethodOrderingEntrypoint]
internal class GeneratedStateMethodOrdering : global::Sparkitect.GameState.StateMethodOrdering
{
    public override void ConfigureOrdering(global::System.Collections.Generic.HashSet<global::Sparkitect.GameState.OrderingEntry> ordering)
    {

        ordering.Add(new global::Sparkitect.GameState.OrderingEntry(
            (global::GameStateTest.TestModule, "init"),
            (global::GameStateTest.AnotherModule.Identification, "process")));

        ordering.Add(new global::Sparkitect.GameState.OrderingEntry(
            (global::GameStateTest.TestModule.Identification, "cleanup"),
            (global::GameStateTest.TestModule.Identification, "update")));

        ordering.Add(new global::Sparkitect.GameState.OrderingEntry(
            (global::GameStateTest.TestModule.Identification, "init"),
            (global::GameStateTest.TestModule.Identification, "update")));

    }
}