using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using Sparkitect.RiderPlugin.References;

namespace Sparkitect.RiderPlugin.Registrations;

/// <summary>
/// The single shared detection point: reads category / marker / <c>[RegisteredFrom]</c> once and
/// constructs the matching <see cref="Registration" /> subtype. Category and mod are taken from reliable
/// structured metadata only — the forward <see cref="RegistrationMarkerPredicate" /> / <see cref="RegistrationKey" />
/// pivots, never namespace or short-name guessing. This factory plus the subtypes' own payload logic are
/// the only places category→subtype mapping lives; no category-specific branching is scattered elsewhere.
/// </summary>
public static class RegistrationFactory
{
    /// <summary>
    /// External-function registries are <c>External = true</c> and register methods rather than types;
    /// their leaves carry an optional Type(Member) payload. Held as the one explicit category set so the
    /// subtype mapping stays in this single location.
    /// </summary>
    private static readonly string[] ExternalCategories =
        ["perframe_function", "transition_function", "ecs_system"];

    /// <summary>
    /// Detects a C# registration from an id string literal: confirms the carrying attribute is a
    /// registration attribute (forward marker present), reconstructs the <see cref="RegistrationKey" />
    /// from category + csproj <c>ModId</c>, and returns the subtype for that category. Returns null when
    /// the literal is not a registration anchor.
    /// </summary>
    public static Registration? FromCSharpLiteral(ICSharpLiteralExpression literal, string idString)
    {
        var argument = CSharpArgumentNavigator.GetByValue(literal);
        if (argument == null)
            return null;

        var attribute = AttributeNavigator.GetByArgument(argument);
        if (attribute == null)
            return null;

        var attributeType = attribute.TypeReference?.Resolve().DeclaredElement as ITypeElement;
        if (!RegistrationMarkerPredicate.IsRegistrationAttribute(attributeType))
            return null;

        var category = RegistrationKey.MarkerCategory(attributeType!);
        if (string.IsNullOrEmpty(category))
            return null;

        var registeredType = attribute.GetContainingTypeElement(false);
        if (registeredType == null)
            return null;

        var modId = SparkitectModId.Resolve(registeredType);
        if (string.IsNullOrEmpty(modId))
            return null;

        var key = RegistrationKey.FromAttribute(attributeType!, idString, modId!);
        if (key == null)
            return null;

        return Create(category!, literal, key.Value);
    }

    /// <summary>
    /// Detects a YAML registration from an entry-key scalar using already-resolved reliable inputs
    /// (mod, category, entry id). A YAML registration is always resource-backed.
    /// </summary>
    public static Registration? FromYamlEntry(ITreeNode entryKey, string modId, string category, string entryId)
    {
        var key = RegistrationKey.FromYaml(modId, category, entryId);
        return key == null ? null : new ResourceRegistration(entryKey, key.Value);
    }

    /// <summary>The one category→subtype mapping. New categories extend this single switch.</summary>
    private static Registration Create(string category, ITreeNode anchor, RegistrationKey key)
    {
        if (System.Array.IndexOf(ExternalCategories, category) >= 0)
            return new ExternalRegistration(anchor, key);

        return new TypeRegistration(anchor, key);
    }
}
