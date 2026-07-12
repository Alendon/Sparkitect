using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace Sparkitect.Debug;

/// <summary>
/// The debug channel's composition-inclusion gate. When the <c>debug_channel</c> setting is off the
/// debug module is dropped from the module universe the composer sees, so its <c>ActivatesWith =&gt; [Core]</c>
/// auto-activation never pulls it into any composed set — off ⇒ absent from every state's set, not merely
/// runtime-inert. Deliberately debug-specific, not a general setting-gated-module mechanism.
/// </summary>
internal static class DebugModuleGate
{
    /// <summary>The debug channel module id (generated from its module registration).</summary>
    public static Identification ModuleId => StateModuleID.Sparkitect.DebugChannel;

    /// <summary>
    /// Removes the debug module from <paramref name="registeredModules"/> when the channel is disabled, so
    /// the composer never auto-activates it into any state. No-op when the channel is enabled or the module
    /// is already absent. Generic over the module value type so it applies equally to the game-state
    /// manager's registered-module map and the composer's plain-data inputs.
    /// </summary>
    /// <typeparam name="TModule">The registered-module value type.</typeparam>
    /// <param name="registeredModules">The module universe to gate, keyed by module id.</param>
    /// <param name="channelEnabled">Whether the debug channel setting is on.</param>
    public static void ExcludeWhenDisabled<TModule>(
        IDictionary<Identification, TModule> registeredModules, bool channelEnabled)
    {
        if (!channelEnabled)
        {
            registeredModules.Remove(ModuleId);
        }
    }
}
