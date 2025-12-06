using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator;

public static class DisplayFormats
{
    public static SymbolDisplayFormat NamespaceAndType =>
        SymbolDisplayFormat.FullyQualifiedFormat.WithGenericsOptions(SymbolDisplayGenericsOptions.None)
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted)
            .RemoveMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
}