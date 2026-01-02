using JetBrains.Annotations;
using Sparkitect.DI.GeneratorAttributes;

namespace Sparkitect.GameState;

/// <summary>
/// Marks a static method as a state function. Must be combined with a scheduling attribute.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class StateFunctionAttribute(string key) : Attribute
{
    /// <summary>
    /// Gets the unique key identifying this function within its module/state.
    /// </summary>
    public string Key { get; } = key;
}

/// <summary>
/// Schedules a state function to run every frame while the state is active.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class PerFrameAttribute : Attribute;

/// <summary>
/// Schedules a state function to run once when the module/state is created.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnCreateAttribute : Attribute;

/// <summary>
/// Schedules a state function to run once when the module/state is destroyed.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnDestroyAttribute : Attribute;

/// <summary>
/// Schedules a state function to run when the state becomes the active leaf.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnFrameEnterAttribute : Attribute;

/// <summary>
/// Schedules a state function to run when the state stops being the active leaf.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnFrameExitAttribute : Attribute;

/// <summary>
/// Marks an interface as having a state-function-exclusive facade. The facade interface is only accessible
/// within state functions and not through normal DI resolution.
/// </summary>
/// <typeparam name="TFacade">The exclusive facade interface type for state functions.</typeparam>
[PublicAPI]
[AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
public sealed class StateFacadeAttribute<TFacade> : FacadeMarkerAttribute<TFacade> where TFacade : class;

/// <summary>
/// Non-generic marker attribute for StateFacade entrypoint discovery. Applied by source generators.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class StateFacadeAttribute : Attribute;

/// <summary>
/// Marks a class as a state-scoped service. Triggers source generation of service factory and module configurator.
/// The service is created when the module is activated and destroyed when deactivated.
/// </summary>
/// <typeparam name="TInterface">The interface type this service implements.</typeparam>
/// <typeparam name="TModule">The module this service belongs to.</typeparam>
[PublicAPI]
[FactoryGenerationType(FactoryGenerationType.Service)]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class StateServiceAttribute<TInterface, TModule> : Attribute, IFactoryMarker<TInterface>
    where TInterface : class
    where TModule : IStateModule;

