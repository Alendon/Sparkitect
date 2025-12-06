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
            global::GameStateTest.DesktopState.Identification,
            global::GameStateTest.DesktopState.Initialize_Key,
            typeof(global::GameStateTest.DesktopState.desktop_initWrapper),
            global::Sparkitect.GameState.StateMethodSchedule.OnFrameEnter);

    }
}