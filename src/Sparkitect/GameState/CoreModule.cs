using JetBrains.Annotations;
using Sparkitect.Modding;
using Sparkitect.Utils;

namespace Sparkitect.GameState;

[PublicAPI]
[ModuleRegistry.RegisterModule("core")]
public sealed partial class CoreModule : IStateModule
{
    public const string Key_Remove = "remove";
    public const string Key_Before = "before";
    public const string Key_Add = "add";
    public const string Key_After = "after";

    public static IReadOnlyList<Type> ExposedServices =>
    [
        typeof(IModManager),
        typeof(ICliArgumentHandler),
        typeof(IGameStateManager),
        typeof(IIdentificationManager)
    ];


    [Transition(TransitionTrigger.Removed)]
    public static void OnRemove(TransitionContext ctx)
    {
        // Core registry/mod deactivation logic placeholder
        _ = ctx;
    }

    [Transition(TransitionTrigger.UnchangedBefore)]
    public static void OnUnchangedBefore(TransitionContext ctx)
    {
        // Core pre-rebuild work placeholder
        _ = ctx;
    }

    [Transition(TransitionTrigger.Add)]
    public static void OnAdd(TransitionContext ctx)
    {
        // Core registry/mod activation logic placeholder
        _ = ctx;
    }

    [Transition(TransitionTrigger.UnchangedAfter)]
    public static void OnUnchangedAfter(TransitionContext ctx)
    {
        // Core post-rebuild work placeholder
        _ = ctx;
    }
}

