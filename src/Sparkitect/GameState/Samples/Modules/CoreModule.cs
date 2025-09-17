using JetBrains.Annotations;
using Sparkitect.GameState;

namespace Sparkitect.GameState.Samples.Modules;

[PublicAPI]
[ModuleRegistry.Register("core")]
public sealed partial class CoreModule : IStateModule
{
    public const string Key_Remove = "remove";
    public const string Key_Before = "before";
    public const string Key_Add = "add";
    public const string Key_After = "after";

    [Transition(TransitionTrigger.Removed, Key_Remove)]
    public static void OnRemove(TransitionContext ctx)
    {
        // Core registry/mod deactivation logic placeholder
        _ = ctx;
    }

    [Transition(TransitionTrigger.UnchangedBefore, Key_Before)]
    public static void OnUnchangedBefore(TransitionContext ctx)
    {
        // Core pre-rebuild work placeholder
        _ = ctx;
    }

    [Transition(TransitionTrigger.Add, Key_Add)]
    public static void OnAdd(TransitionContext ctx)
    {
        // Core registry/mod activation logic placeholder
        _ = ctx;
    }

    [Transition(TransitionTrigger.UnchangedAfter, Key_After)]
    public static void OnUnchangedAfter(TransitionContext ctx)
    {
        // Core post-rebuild work placeholder
        _ = ctx;
    }
}

