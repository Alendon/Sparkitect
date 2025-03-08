using JetBrains.Annotations;

namespace Sparkitect.DI;

public abstract class SingletonBaseAttribute : Attribute
{
    public abstract Type GetImplementationType();
    public abstract Type GetInterfaceType();
    public abstract string[] GetPositiveFilters();
    public abstract string[] GetNegativeFilters();
}

[MeansImplicitUse]
[PublicAPI]
public sealed class SingletonAttribute<TThis, TInterface> : SingletonBaseAttribute
    where TThis : TInterface
    where TInterface : class
{
    private readonly string[] _positiveFilters;
    private readonly string[] _negativeFilters;
    
    /// <summary>
    /// Instantiate a new <see cref="SingletonAttribute{TThis, TInterface}" /> instance without filtering
    /// </summary>
    public SingletonAttribute(string[]? positiveFilters = null, string[]? negativeFilters = null)
    {
        _positiveFilters = positiveFilters ?? [];
        _negativeFilters = negativeFilters ?? [];
    }


    public override Type GetImplementationType()
    {
        return typeof(TThis);
    }

    public override Type GetInterfaceType()
    {
        return typeof(TInterface);
    }

    public override string[] GetPositiveFilters()
    {
        return _positiveFilters;
    }

    public override string[] GetNegativeFilters()
    {
        return _negativeFilters;
    }
}