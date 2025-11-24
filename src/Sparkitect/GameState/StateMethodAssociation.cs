using Sparkitect.DI;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Marks state method association configurators for source generator discovery.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class StateMethodAssociationEntrypointAttribute : Attribute;

/// <summary>
/// Defines the scheduling type for state functions.
/// </summary>
public enum StateMethodSchedule
{
    /// <summary>
    /// Execute every frame while state is active.
    /// </summary>
    PerFrame,

    /// <summary>
    /// Execute once when module/state is created.
    /// </summary>
    OnCreate,

    /// <summary>
    /// Execute once when module/state is destroyed.
    /// </summary>
    OnDestroy,

    /// <summary>
    /// Execute when state becomes active leaf.
    /// </summary>
    OnFrameEnter,

    /// <summary>
    /// Execute when state stops being active leaf.
    /// </summary>
    OnFrameExit
}

/// <summary>
/// Builder for state method associations. Maps (parent, method key, schedule) to wrapper types.
/// Used internally by source generators.
/// </summary>
public sealed class StateMethodAssociationBuilder
{
    private readonly Dictionary<(Identification ParentId, string MethodKey, StateMethodSchedule Schedule), Type> _registrations = new();

    /// <summary>
    /// Adds a state method wrapper type association.
    /// </summary>
    /// <param name="parentId">The parent module or state identification.</param>
    /// <param name="methodKey">The function key.</param>
    /// <param name="wrapperType">The generated wrapper type.</param>
    /// <param name="schedule">The scheduling type.</param>
    public void Add(Identification parentId, string methodKey, Type wrapperType, StateMethodSchedule schedule)
    {
        _registrations[(parentId, methodKey, schedule)] = wrapperType;
    }

    /// <summary>
    /// Removes a state method association.
    /// </summary>
    /// <param name="parentId">The parent identification.</param>
    /// <param name="methodKey">The function key.</param>
    /// <param name="schedule">The scheduling type.</param>
    /// <returns>True if removed.</returns>
    public bool Remove(Identification parentId, string methodKey, StateMethodSchedule schedule)
    {
        return _registrations.Remove((parentId, methodKey, schedule));
    }

    /// <summary>
    /// Clears all associations.
    /// </summary>
    public void Clear()
    {
        _registrations.Clear();
    }

    /// <summary>
    /// Builds the final association dictionary.
    /// </summary>
    /// <returns>Read-only dictionary of associations.</returns>
    public IReadOnlyDictionary<(Identification ParentId, string MethodKey, StateMethodSchedule Schedule), Type> Build()
    {
        return _registrations;
    }
}

/// <summary>
/// Base class for state method association configurators. Implementations source-generated per module/state
/// to register their state function wrapper types.
/// </summary>
public abstract class StateMethodAssociation : IConfigurationEntrypoint<StateMethodAssociationEntrypointAttribute>
{
    /// <summary>
    /// Configures state method associations by adding wrapper types to the builder.
    /// </summary>
    /// <param name="builder">The association builder.</param>
    public abstract void Configure(StateMethodAssociationBuilder builder);
}
