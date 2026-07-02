using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.Modding;

/// <summary>
/// Shared helper for source generators that need to ask "is this type identified, as visible to a
/// SOURCE GENERATOR?". Every registered/identified concrete declares <c>: IHasIdentification</c>
/// explicitly in user source, so a direct <see cref="INamedTypeSymbol.AllInterfaces"/> scan is
/// observable across generators within the same compilation pass and is sufficient.
/// </summary>
internal static class IdentificationContract
{
    private const string IHasIdentificationFqn = "Sparkitect.Modding.IHasIdentification";

    /// <summary>
    /// Returns true when <paramref name="type"/> directly carries <c>IHasIdentification</c> in
    /// <see cref="INamedTypeSymbol.AllInterfaces"/> (visible in user-source partials).
    /// </summary>
    public static bool IsIdentified(INamedTypeSymbol type)
    {
        foreach (var iface in type.AllInterfaces)
        {
            if (iface.ToDisplayString(DisplayFormats.NamespaceAndType) == IHasIdentificationFqn)
                return true;
        }

        return false;
    }
}
