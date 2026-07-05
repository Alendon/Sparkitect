using JetBrains.Annotations;
using Sparkitect.Metadata;
using Sparkitect.Modding;

namespace Sparkitect.Stateless;

/// <summary>
/// Non-generic base for "run before" ordering constraints. Use the generic
/// <see cref="OrderBeforeAttribute{TOther}"/> in mod code; this base exists for source-generator
/// and non-generic access to the resolved target.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
[PublicAPI]
public abstract class OrderBeforeAttribute() : MetadataParameterAttribute
{
    /// <summary>The identification of the function this constraint orders against.</summary>
    public abstract Identification Other { get; }

    /// <summary>When true, the constraint is dropped if <see cref="Other"/> is absent instead of throwing.</summary>
    public abstract bool Optional { get; }

    /// <summary>
    /// Applies this ordering constraint to the execution graph.
    /// Adds an edge from functionId to Other (this function runs before Other).
    /// </summary>
    public void Apply(IExecutionGraphBuilder builder, Identification functionId)
    {
        builder.AddEdge(functionId, Other, Optional);
    }
}

/// <summary>
/// Non-generic base for "run after" ordering constraints. Use the generic
/// <see cref="OrderAfterAttribute{TOther}"/> in mod code; this base exists for source-generator
/// and non-generic access to the resolved target.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
[PublicAPI]
public abstract class OrderAfterAttribute() : MetadataParameterAttribute
{
    /// <summary>The identification of the function this constraint orders against.</summary>
    public abstract Identification Other { get; }

    /// <summary>When true, the constraint is dropped if <see cref="Other"/> is absent instead of throwing.</summary>
    public abstract bool Optional { get; }

    /// <summary>
    /// Applies this ordering constraint to the execution graph.
    /// Adds an edge from Other to functionId (this function runs after Other).
    /// </summary>
    public void Apply(IExecutionGraphBuilder builder, Identification functionId)
    {
        builder.AddEdge(Other, functionId, Optional);
    }
}


/// <summary>
/// Orders the annotated function or system group to run before <typeparamref name="TOther"/>.
/// Set <see cref="IsOptional"/> to tolerate a missing target.
/// </summary>
/// <typeparam name="TOther">The target that carries an <see cref="IHasIdentification"/>.</typeparam>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
[PublicAPI]
public sealed class OrderBeforeAttribute<TOther>() : OrderBeforeAttribute
    where TOther : IHasIdentification
{
    /// <inheritdoc/>
    public override Identification Other => TOther.Identification;

    /// <inheritdoc/>
    public override bool Optional => IsOptional;

    /// <summary>When true, the constraint is skipped if <typeparamref name="TOther"/> is not present.</summary>
    public bool IsOptional { get; set; } = false;
}

/// <summary>
/// Orders the annotated function or system group to run after <typeparamref name="TOther"/>.
/// Set <see cref="IsOptional"/> to tolerate a missing target.
/// </summary>
/// <typeparam name="TOther">The target that carries an <see cref="IHasIdentification"/>.</typeparam>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
[PublicAPI]
public sealed class OrderAfterAttribute<TOther>() : OrderAfterAttribute
    where TOther : IHasIdentification
{
    /// <inheritdoc/>
    public override Identification Other => TOther.Identification;

    /// <inheritdoc/>
    public override bool Optional => IsOptional;

    /// <summary>When true, the constraint is skipped if <typeparamref name="TOther"/> is not present.</summary>
    public bool IsOptional { get; set; } = false;
}