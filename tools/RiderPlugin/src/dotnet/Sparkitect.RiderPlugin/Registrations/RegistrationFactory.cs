using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
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

    /// <summary>
    /// Detects the registration owning a generated leaf id property — the reverse direction used by Go to
    /// Registration. Reads the leaf's <c>[RegisteredFrom]</c> owner edge, then for a C# owner locates the
    /// authoritative id-string literal (the registration attribute argument that reconstructs the same
    /// <see cref="RegistrationKey" />) on the owning type and returns the category's subtype anchored on it.
    /// Returns null for a resource-file (YAML) owner — that coordinate has no in-source C# anchor and is
    /// navigated via <see cref="RegisteredFromReader" /> directly. Returns null when no owner is present.
    /// </summary>
    public static Registration? FromLeaf(IProperty leaf)
    {
        var owner = RegisteredFromReader.Read(leaf);
        if (owner?.Type == null)
            return null;

        var key = RegistrationKey.FromLeafProperty(leaf);
        if (key == null)
            return null;

        var literal = FindRegistrationLiteral(owner.Type, key.Value);
        if (literal == null)
            return null;

        var category = OwnerCategory(literal);
        return category == null ? null : Create(category, literal, key.Value);
    }

    /// <summary>The one category→subtype mapping. New categories extend this single switch.</summary>
    private static Registration Create(string category, ITreeNode anchor, RegistrationKey key)
    {
        if (System.Array.IndexOf(ExternalCategories, category) >= 0)
            return new ExternalRegistration(anchor, key);

        return new TypeRegistration(anchor, key);
    }

    /// <summary>
    /// Locates the registration id-string literal on the owning type: the registration-attribute argument
    /// whose value reconstructs the same key as the leaf. Attributes on the type's members are descendants
    /// of the type declaration, so a member-borne registration (method/property/value, stateless, ECS) is
    /// found by the same walk as a type-borne one.
    /// </summary>
    private static ICSharpLiteralExpression? FindRegistrationLiteral(
        ITypeElement ownerType, RegistrationKey leafKey)
    {
        foreach (var declaration in ownerType.GetDeclarations())
        foreach (var literal in declaration.Descendants<ICSharpLiteralExpression>().Collect())
        {
            if (!literal.IsConstantValue() || !literal.ConstantValue.IsString())
                continue;

            var idString = literal.ConstantValue.AsString();
            if (string.IsNullOrEmpty(idString))
                continue;

            var registration = FromCSharpLiteral(literal, idString!);
            if (registration != null && registration.Key.AsTuple() == leafKey.AsTuple())
                return literal;
        }

        return null;
    }

    /// <summary>The marker category carried by the registration attribute owning <paramref name="literal" />.</summary>
    private static string? OwnerCategory(ICSharpLiteralExpression literal)
    {
        var argument = CSharpArgumentNavigator.GetByValue(literal);
        var attribute = argument == null ? null : AttributeNavigator.GetByArgument(argument);
        var attributeType = attribute?.TypeReference?.Resolve().DeclaredElement as ITypeElement;
        return attributeType == null ? null : RegistrationKey.MarkerCategory(attributeType);
    }
}
