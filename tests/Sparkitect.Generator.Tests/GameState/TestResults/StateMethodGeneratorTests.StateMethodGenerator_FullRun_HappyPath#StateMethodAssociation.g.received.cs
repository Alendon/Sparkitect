//HintName: StateMethodAssociation.g.cs
namespace GameStateTest;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.GameState.StateMethodAssociationEntrypoint]
internal class GeneratedStateMethodAssociation : global::Sparkitect.GameState.StateMethodAssociation
{
    public override void Configure(global::Sparkitect.GameState.StateMethodAssociationBuilder builder)
    {

        builder.Add(
            global::GameStateTest.AnotherModule.Identification,
            "process",
            typeof(global::GameStateTest.AnotherModule.processWrapper),
            global::Sparkitect.GameState.StateMethodSchedule.OnFrameEnter);

        builder.Add(
            global::GameStateTest.TestModule.Identification,
            "cleanup",
            typeof(global::GameStateTest.TestModule.cleanupWrapper),
            global::Sparkitect.GameState.StateMethodSchedule.OnDestroy);

        builder.Add(
            global::GameStateTest.TestModule.Identification,
            "init",
            typeof(global::GameStateTest.TestModule.initWrapper),
            global::Sparkitect.GameState.StateMethodSchedule.OnCreate);

        builder.Add(
            global::GameStateTest.TestModule.Identification,
            "update",
            typeof(global::GameStateTest.TestModule.updateWrapper),
            global::Sparkitect.GameState.StateMethodSchedule.PerFrame);

    }
}