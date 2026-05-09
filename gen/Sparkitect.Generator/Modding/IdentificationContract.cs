using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.Modding;

/// <summary>
/// Shared helper for source generators that need to ask "is this type identified, as visible to a
/// SOURCE GENERATOR?". After Phase 49.3-04 dropped <c>: IHasIdentification</c> from contract
/// interfaces (e.g. <c>IStateModule</c>, <c>IStateDescriptor</c>), final concretes acquire
/// <see cref="IHasIdentification"/> only through <c>RegistryGenerator</c> auto-emit — which is
/// invisible to sibling generators within the same compilation pass. Source generators must
/// therefore widen their identification check to also accept <c>[TypedRegistrationContract]</c>
/// on a base type / implemented interface.
/// </summary>
/// <remarks>
/// <para>Use from source generators only. Analyzers run after the full compilation including SG
/// output, so a plain <c>AllInterfaces.Any(IHasIdentification)</c> check is sufficient there.</para>
/// </remarks>
internal static class IdentificationContract
{
    private const string IHasIdentificationFqn = "Sparkitect.Modding.IHasIdentification";
    private const string TypedRegistrationContractAttributeFqn =
        "Sparkitect.Modding.TypedRegistrationContractAttribute";

    /// <summary>
    /// Returns true when <paramref name="type"/> is observable as identified to a SOURCE
    /// GENERATOR — either it directly carries <c>IHasIdentification</c> in
    /// <see cref="INamedTypeSymbol.AllInterfaces"/> (visible in user-source partials), OR it
    /// derives from a base class / implements an interface (transitively) carrying
    /// <c>[TypedRegistrationContract]</c>, signaling the RegistryGenerator's auto-emit pipeline
    /// will supply <c>IHasIdentification</c> for it.
    /// </summary>
    public static bool IsIdentified(INamedTypeSymbol type)
    {
        // Fast path: direct user-source `: IHasIdentification` is observable here.
        foreach (var iface in type.AllInterfaces)
        {
            if (iface.ToDisplayString(DisplayFormats.NamespaceAndType) == IHasIdentificationFqn)
                return true;
        }

        // Slow path: auto-emit — recognize the [TypedRegistrationContract] signal on the
        // base-class chain or any implemented interface.
        return HasTypedRegistrationContract(type);
    }

    /// <summary>
    /// Returns true when <paramref name="type"/> itself carries <c>[TypedRegistrationContract]</c>,
    /// or any of its implemented interfaces (transitively) or base classes does.
    /// </summary>
    public static bool HasTypedRegistrationContract(INamedTypeSymbol type)
    {
        // Direct on the type.
        if (HasContractAttribute(type)) return true;

        // Walk implemented interfaces (transitive closure via AllInterfaces).
        foreach (var iface in type.AllInterfaces)
        {
            if (HasContractAttribute(iface)) return true;
        }

        // Walk base-class chain.
        for (var baseType = type.BaseType; baseType is not null; baseType = baseType.BaseType)
        {
            if (HasContractAttribute(baseType)) return true;
        }

        return false;
    }

    private static bool HasContractAttribute(INamedTypeSymbol type)
    {
        foreach (var attr in type.GetAttributes())
        {
            if (attr.AttributeClass is { } ac &&
                ac.ToDisplayString(DisplayFormats.NamespaceAndType) == TypedRegistrationContractAttributeFqn)
            {
                return true;
            }
        }
        return false;
    }
}
