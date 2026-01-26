using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Stateless;

// TODO: SG Analyzer - Validate StatelessFunctionAttribute usage:
//   - Method must be static
//   - Method must have exactly one scheduling attribute matching TContext/TRegistry
//   - Method parameters must be DI-resolvable types
//   - Containing type must implement IHasIdentification

//Non generic base class, to be able to pass a generic type without the sub types, when only SG or non CLR compliant usage is  required
public abstract class StatelessFunctionAttribute : Attribute
{
    public abstract string Identifier { get; }
}

/// <summary>
/// Base attribute for all stateless functions. Encodes registry association and contextual data type.
/// Source generators use TRegistry to determine which registry collector receives this function.
/// </summary>
/// <typeparam name="TContext">The contextual data type used by scheduling implementations.</typeparam>
/// <typeparam name="TRegistry">The registry this function belongs to (for SG association).</typeparam>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public abstract class StatelessFunctionAttribute<TContext, TRegistry> : StatelessFunctionAttribute
    where TContext : class
    where TRegistry : IRegistry
{
    public override string Identifier { get; }

    protected StatelessFunctionAttribute(string identifier) => Identifier = identifier;
}

/// <summary>
/// Marks a static method as a per-frame stateless function.
/// Executes every frame while the owning state/module is active.
/// Must be combined with a scheduling attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class PerFrameFunctionAttribute(string identifier)
    : StatelessFunctionAttribute<PerFrameContext, PerFrameRegistry>(identifier);

/// <summary>
/// Marks a static method as a transition stateless function.
/// Must be combined with a scheduling attribute (OnCreate, OnDestroy, etc.).
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class TransitionFunctionAttribute(string identifier)
    : StatelessFunctionAttribute<TransitionContext, TransitionRegistry>(identifier);

/// <summary>
/// Explicitly specifies the parent (owner) of a stateless function.
/// Takes priority over containing class's IHasIdentification.
/// </summary>
/// <typeparam name="TOwner">The owner type implementing IHasIdentification.</typeparam>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ParentIdAttribute<TOwner> : Attribute
    where TOwner : IHasIdentification;

/// <summary>
/// Marker base for scheduling parameter attributes. Used by analyzers to validate
/// that these attributes are only applied to stateless function methods.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
public abstract class SchedulingParameterAttribute : Attribute;