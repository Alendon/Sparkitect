using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Marks a class as an entry state selector entrypoint for automatic discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class EntryStateSelectorEntrypointAttribute : Attribute;

/// <summary>
/// Configuration entrypoint for selecting the initial active state after engine initialization.
/// Implementations are discovered and invoked to determine which state to enter first.
/// </summary>
public interface IEntryStateSelector : IConfigurationEntrypoint<EntryStateSelectorEntrypointAttribute>
{
    /// <summary>
    /// Selects the entry state to activate after root state initialization.
    /// </summary>
    /// <param name="container">The root DI container.</param>
    /// <returns>The identification of the state to enter first (cannot be Root).</returns>
    public Identification SelectEntryState(ICoreContainer container);
}

