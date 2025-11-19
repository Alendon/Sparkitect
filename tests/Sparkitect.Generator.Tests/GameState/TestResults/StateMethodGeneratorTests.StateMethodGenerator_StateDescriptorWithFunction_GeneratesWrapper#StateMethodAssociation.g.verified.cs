//HintName: StateMethodAssociation.g.cs
namespace GameStateTest;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.GameState.StateMethodAssociationEntrypoint]
internal class GeneratedStateMethodAssociation : global::Sparkitect.GameState.StateMethodAssociation
{
    public override void Configure(global::Sparkitect.GameState.StateMethodAssociationBuilder builder)
    {

        builder.Add(
            global::GameStateTest.DesktopState.Identification,
            "desktop_init",
            typeof(global::GameStateTest.DesktopState.desktop_initWrapper),
            global::Sparkitect.GameState.StateMethodSchedule.OnStateEnter);

    }
}