using System.Linq;
using System.Text;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Util;

namespace Sparkitect.RiderPlugin.References;

/// <summary>
/// Canonical key shared by forward navigation, the reverse index, and the reverse-nav provider.
/// A key is the pair <c>(generated IDs-struct CLR name, leaf member name)</c> — both endpoints can
/// derive this identically, so a forward lookup and a reverse lookup always agree.
/// </summary>
/// <remarks>
/// The IDs-struct CLR name is reconstructed deterministically from reliable inputs:
/// <c>{Mod}{Category}IDs</c> under the <c>.CompilerGenerated.IdExtensions.</c> namespace, where
/// <c>{Mod}</c> is the Pascal-cased csproj <c>&lt;ModId&gt;</c> and <c>{Category}</c> is the Pascal-cased
/// registration category. Both segments come from structured metadata (the forward marker's category
/// argument and the csproj ModId), never from namespace/short-name guessing.
/// </remarks>
public readonly struct RegistrationKey
{
    private const string IdExtensionsNamespaceSuffix = ".CompilerGenerated.IdExtensions.";
    private const string IdsStructSuffix = "IDs";
    private const string RegistrationMarkerFullName = "Sparkitect.Modding.RegistrationMarkerAttribute";

    /// <summary>CLR name of the generated <c>{Mod}{Category}IDs</c> struct.</summary>
    public string IdsStructClrName { get; }

    /// <summary>Pascal-cased leaf member name (e.g. <c>ClearColor</c>).</summary>
    public string MemberName { get; }

    public RegistrationKey(string idsStructClrName, string memberName)
    {
        IdsStructClrName = idsStructClrName;
        MemberName = memberName;
    }

    public (string, string) AsTuple() => (IdsStructClrName, MemberName);

    /// <summary>
    /// Derives the key from a C# registration attribute. The category comes from the forward
    /// <c>RegistrationMarkerAttribute</c> carried by the attribute type itself; the mod segment from the
    /// owning project's csproj <c>&lt;ModId&gt;</c>; the leaf from the id string literal. No category or
    /// mod-prefix guessing.
    /// </summary>
    public static RegistrationKey? FromAttribute(ITypeElement attributeType, string idString, string modId)
    {
        var category = MarkerCategory(attributeType);
        if (string.IsNullOrEmpty(category))
            return null;

        return FromReliableInputs(modId, category!, idString);
    }

    /// <summary>
    /// Derives the key from a resource-file entry using already-resolved reliable inputs: the owning
    /// mod's csproj <c>&lt;ModId&gt;</c>, the registry's declared category, and the entry id. Produces the
    /// same key a C# attribute for the same registration would.
    /// </summary>
    public static RegistrationKey? FromYaml(string modId, string category, string entryId) =>
        FromReliableInputs(modId, category, entryId);

    /// <summary>
    /// Derives the key from a runtime string identification triple carried on the debug-channel wire
    /// (mod / category / item names). The wire never carries the numeric <c>Identification</c> — its
    /// numbers are assigned at runtime in registration order and are meaningless to the plugin — so the
    /// engine resolves numeric to string before publishing and the plugin rebuilds the very key a C#
    /// registration attribute or resource entry for the same registration would. A thin wrapper over the
    /// shared reliable-input reconstruction; no key-derivation logic is duplicated.
    /// </summary>
    public static RegistrationKey? FromRuntimeIds(string modId, string category, string item) =>
        FromReliableInputs(modId, category, item);

    /// <summary>
    /// Derives the key from a resolved generated leaf property: its declaring type is the IDs struct
    /// and its short name is the member.
    /// </summary>
    public static RegistrationKey? FromLeafProperty(IProperty property)
    {
        var declaringType = property.GetContainingType();
        if (declaringType == null)
            return null;

        var clrName = declaringType.GetClrName().FullName;
        if (string.IsNullOrEmpty(clrName) || !declaringType.ShortName.EndsWith(IdsStructSuffix))
            return null;

        return new RegistrationKey(clrName, property.ShortName);
    }

    /// <summary>The category argument of the attribute type's forward <c>RegistrationMarkerAttribute</c>, or null.</summary>
    public static string? MarkerCategory(ITypeElement attributeType)
    {
        var instances = attributeType.GetAttributeInstances(
            new ClrTypeName(RegistrationMarkerFullName), AttributesSource.Self);
        foreach (var instance in instances)
        {
            var value = instance.PositionParameter(0);
            if (!value.IsBadValue && value.IsConstant && value.ConstantValue.IsString())
                return value.ConstantValue.AsString();
        }

        return null;
    }

    /// <summary>
    /// Assembles the IDs-struct CLR name from reliable inputs — the retained deterministic reconstruction
    /// (D-49): <c>{PascalMod}.CompilerGenerated.IdExtensions.{PascalMod}{PascalCategory}IDs</c>, member =
    /// <c>SnakeToPascal(idString)</c>. Mirrors the generator's <c>StringCase.ToPascalCase(ModId)</c> +
    /// <c>ToPascalCase(category)</c> struct naming.
    /// </summary>
    private static RegistrationKey? FromReliableInputs(string modId, string category, string idString)
    {
        if (string.IsNullOrEmpty(modId) || string.IsNullOrEmpty(category))
            return null;

        var modPascal = SnakeToPascal(modId);
        var categoryPascal = SnakeToPascal(category);
        if (string.IsNullOrEmpty(modPascal) || string.IsNullOrEmpty(categoryPascal))
            return null;

        var member = SnakeToPascal(idString);
        if (string.IsNullOrEmpty(member))
            return null;

        var structName = modPascal + categoryPascal + IdsStructSuffix;
        var idsStruct = modPascal + IdExtensionsNamespaceSuffix + structName;
        return new RegistrationKey(idsStruct, member);
    }

    /// <summary>The deterministic snake_case → PascalCase transform retained on the reliable-input path (D-49).</summary>
    public static string SnakeToPascal(string snake)
    {
        var builder = new StringBuilder(snake.Length);
        var capitalizeNext = true;
        foreach (var ch in snake)
        {
            if (ch == '_')
            {
                capitalizeNext = true;
                continue;
            }

            builder.Append(capitalizeNext ? char.ToUpperInvariant(ch) : ch);
            capitalizeNext = false;
        }

        return builder.ToString();
    }
}
