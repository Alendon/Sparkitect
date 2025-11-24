using Sparkitect.DI;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Marks state method ordering configurators for source generator discovery.
/// </summary>
public class IStateMethodOrderingEntrypointAttribute : Attribute;

/// <summary>
/// Base class for state method ordering configurators. Implementations source-generated per module/state
/// to specify execution order constraints between state functions.
/// </summary>
public abstract class StateMethodOrdering : IConfigurationEntrypoint<IStateMethodOrderingEntrypointAttribute>
{
    /// <summary>
    /// Configures ordering constraints for state functions.
    /// </summary>
    /// <param name="ordering">Set to add ordering entries to.</param>
    public abstract void ConfigureOrdering(HashSet<OrderingEntry> ordering);
}

/// <summary>
/// Defines an ordering constraint: Before function must execute before After function.
/// </summary>
/// <param name="Before">The (parent ID, method key) tuple for the function that executes first.</param>
/// <param name="After">The (parent ID, method key) tuple for the function that executes after.</param>
public record OrderingEntry((Identification Parent, string Method) Before, (Identification Parent, string Method) After);