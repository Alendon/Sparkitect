using Sparkitect.Modding;

namespace Sparkitect.Stateless;

// TODO: SG Analyzer - Validate SchedulingParameterAttribute usage:
//   - Must only be applied to methods with a StatelessFunctionAttribute
//   - Must be combined with a matching scheduling attribute

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public abstract class OrderBeforeAttribute() : Attribute
{
    public abstract Identification Other { get; }
    public abstract bool Optional { get; }
}

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public abstract class OrderAfterAttribute() : Attribute
{
    public abstract Identification Other { get; }
    public abstract bool Optional { get; }
}


[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class OrderBeforeAttribute<TOtherFunction>() : OrderBeforeAttribute
    where TOtherFunction : IStatelessFunction, IHasIdentification
{
    public override Identification Other => TOtherFunction.Identification;
    public override bool Optional => IsOptional;
    public bool IsOptional { get; set; } = false;
}

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class OrderAfterAttribute<TOtherFunction>() : OrderAfterAttribute
    where TOtherFunction : IStatelessFunction, IHasIdentification
{
    public override Identification Other => TOtherFunction.Identification;
    
    public override bool Optional => IsOptional;
    public bool IsOptional { get; set; } = false;
}