using JetBrains.Annotations;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.GameState;
using Sparkitect.Metadata;
using Sparkitect.Modding;

namespace Sparkitect.Stateless;


/// <summary>
/// Non-generic base for all stateless-function attributes. Exposes the registration identifier
/// without the generic context/registry type parameters, for source-generator access.
/// </summary>
[PublicAPI]
public abstract class StatelessFunctionAttribute : Attribute
{
    /// <summary>The unique identifier under which the function is registered.</summary>
    public abstract string Identifier { get; }
}

/// <summary>
/// Base attribute for all stateless functions. Encodes registry association and contextual data type.
/// Source generators use TRegistry to determine which registry collector receives this function.
/// </summary>
/// <typeparam name="TContext">The contextual data type used by scheduling implementations.</typeparam>
/// <typeparam name="TRegistry">The registry this function belongs to (for SG association).</typeparam>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
[PublicAPI]
public abstract class StatelessFunctionAttribute<TContext, TRegistry> : StatelessFunctionAttribute
    where TContext : class
    where TRegistry : IRegistry
{
    /// <inheritdoc/>
    public override string Identifier { get; }

    /// <summary>Creates the attribute with the registration identifier for this function.</summary>
    protected StatelessFunctionAttribute(string identifier) => Identifier = identifier;
}

/// <summary>
/// Marks a static method as a per-frame stateless function.
/// Executes every frame while the owning state/module is active.
/// Must be combined with a scheduling attribute.
/// </summary>
[FacadeCategoryMapping<StateFacadeAttribute>]
[RegistrationMarker("perframe_function")]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
[PublicAPI]
[MeansImplicitUse]
public sealed class PerFrameFunctionAttribute(string identifier)
    : StatelessFunctionAttribute<PerFrameContext, PerFrameRegistry>(identifier);

/// <summary>
/// Marks a static method as a transition stateless function.
/// Must be combined with a scheduling attribute (OnCreate, OnDestroy, etc.).
/// </summary>
[FacadeCategoryMapping<StateFacadeAttribute>]
[RegistrationMarker("transition_function")]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
[PublicAPI]
[MeansImplicitUse]
public sealed class TransitionFunctionAttribute(string identifier)
    : StatelessFunctionAttribute<TransitionContext, TransitionRegistry>(identifier);

/// <summary>
/// Abstract base for parent ID attributes. Carries the resolved Identification of the parent.
/// </summary>
[PublicAPI]
public abstract class ParentIdAttribute : MetadataParameterAttribute
{
    /// <summary>The resolved identification of the declared parent (owner).</summary>
    public abstract Identification Other { get; }
}

/// <summary>
/// Explicitly specifies the parent (owner) of a stateless function or system group.
/// On methods: takes priority over containing class's IHasIdentification.
/// On classes: declares group-to-group parentage for system group hierarchy.
/// </summary>
/// <typeparam name="TOwner">The owner type implementing IHasIdentification.</typeparam>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[PublicAPI]
public sealed class ParentIdAttribute<TOwner> : ParentIdAttribute
    where TOwner : IHasIdentification
{
    /// <inheritdoc/>
    public override Identification Other => TOwner.Identification;
}