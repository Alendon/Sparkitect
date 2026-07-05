using JetBrains.Annotations;
using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Debug;

/// <summary>
/// Foundation marker interface for a debug-channel entry contract; deliberately empty (mirrors
/// <see cref="Sparkitect.Graphics.RenderGraph.IPass"/>). Does NOT extend <see cref="IHasIdentification"/>
/// — its <c>static abstract Identification</c> cannot forward through an abstract base, so each concrete
/// entry implements it directly. An entry is a unit of debug data a mod publishes on the channel.
/// </summary>
[PublicAPI]
public interface IDebugEntry;

/// <summary>
/// The extensible entry point mods contribute debug-channel entries into, an engine-declared
/// <see cref="RegistryAttribute"/> mirroring <see cref="Sparkitect.Graphics.RenderGraph.RenderPassRegistry"/>.
/// Owned by <see cref="DebugChannelModule"/>, so it is added and removed with the debug module's lifecycle.
/// In this release exactly one entry ships — the built-in game-state view assembled by the
/// <c>DebugSnapshotBuilder</c> — and no mod entry is registered; the seam stays open for mods
/// (and a future standalone tool) to add their own entries.
/// </summary>
[Registry(Identifier = "debug_entry")]
[PublicAPI]
public partial class DebugEntryRegistry : IRegistry<DebugChannelModule>
{
    private readonly HashSet<Identification> _entries = new();

    /// <summary>The registered debug-entry ids. The channel host reads these to publish each entry.</summary>
    public IReadOnlyCollection<Identification> Entries => _entries;

    /// <summary>
    /// Registers debug entry <typeparamref name="TEntry"/> under <paramref name="id"/>; called by generated
    /// code from the <c>[DebugEntryRegistry.RegisterEntry(...)]</c> attribute, not directly.
    /// </summary>
    [RegistryMethod]
    public void RegisterEntry<TEntry>(Identification id)
        where TEntry : class, IDebugEntry, IHasIdentification
    {
        _entries.Add(id);
    }

    /// <summary>The registry's stable identifier.</summary>
    public static string Identifier => "debug_entry";

    /// <summary>Removes the entry registered under <paramref name="id"/>.</summary>
    public void Unregister(Identification id)
    {
        _entries.Remove(id);
    }
}
