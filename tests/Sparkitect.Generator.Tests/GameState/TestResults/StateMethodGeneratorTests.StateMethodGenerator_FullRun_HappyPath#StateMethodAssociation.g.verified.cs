//HintName: StateMethodAssociation.g.cs
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace GameStateTest;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.GameState.StateMethodAssociationEntrypoint]
internal class GeneratedStateMethodAssociation : global::Sparkitect.GameState.StateMethodAssociation
{
    public override void Configure(global::Sparkitect.GameState.StateMethodAssociationBuilder builder)
    {

        builder.Add(
            global::GameStateTest.AnotherModule.Identification,
            global::GameStateTest.AnotherModule.Process_Key,
            typeof(global::GameStateTest.AnotherModule.processWrapper),
            global::Sparkitect.GameState.StateMethodSchedule.OnFrameEnter);

        builder.Add(
            global::GameStateTest.TestModule.Identification,
            global::GameStateTest.TestModule.Cleanup_Key,
            typeof(global::GameStateTest.TestModule.cleanupWrapper),
            global::Sparkitect.GameState.StateMethodSchedule.OnDestroy);

        builder.Add(
            global::GameStateTest.TestModule.Identification,
            global::GameStateTest.TestModule.Initialize_Key,
            typeof(global::GameStateTest.TestModule.initWrapper),
            global::Sparkitect.GameState.StateMethodSchedule.OnCreate);

        builder.Add(
            global::GameStateTest.TestModule.Identification,
            global::GameStateTest.TestModule.Update_Key,
            typeof(global::GameStateTest.TestModule.updateWrapper),
            global::Sparkitect.GameState.StateMethodSchedule.PerFrame);

    }
}