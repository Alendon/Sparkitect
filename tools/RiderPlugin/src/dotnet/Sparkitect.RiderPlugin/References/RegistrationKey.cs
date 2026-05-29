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
public readonly struct RegistrationKey
{
    private const string IdExtensionsNamespaceSuffix = ".CompilerGenerated.IdExtensions.";
    private const string RegistrySuffix = "Registry";
    private const string IdsStructSuffix = "IDs";
    private const string RegistryAttributeFullName = "Sparkitect.Modding.RegistryAttribute";

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
    /// Derives the key from a registration attribute: the type it decorates supplies the mod prefix,
    /// the attribute's containing registry type supplies the category, and the id string literal
    /// supplies the leaf name.
    /// </summary>
    public static RegistrationKey? FromAttribute(ITypeElement registeredType, ITypeElement attributeType, string idString)
    {
        var idsStruct = BuildIdsStructClrName(registeredType, attributeType);
        if (idsStruct == null)
            return null;

        var member = SnakeToPascal(idString);
        if (string.IsNullOrEmpty(member))
            return null;

        return new RegistrationKey(idsStruct, member);
    }

    /// <summary>
    /// Derives the key from a resource-file entry: the owning mod supplies the prefix, the registry
    /// CLR name (from the file's top-level method key) supplies the category, and the entry id supplies
    /// the leaf name. Produces the same key a C# attribute for the same registration would.
    /// </summary>
    public static RegistrationKey? FromYaml(string modPrefix, string registryClrName, string entryId)
    {
        if (string.IsNullOrEmpty(modPrefix) || string.IsNullOrEmpty(registryClrName))
            return null;

        var category = CategoryFromRegistryClrName(registryClrName);
        if (string.IsNullOrEmpty(category))
            return null;

        var member = SnakeToPascal(entryId);
        if (string.IsNullOrEmpty(member))
            return null;

        var structName = modPrefix + category + IdsStructSuffix;
        var idsStruct = modPrefix + IdExtensionsNamespaceSuffix + structName;
        return new RegistrationKey(idsStruct, member);
    }

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

    private static string? BuildIdsStructClrName(ITypeElement registeredType, ITypeElement attributeType)
    {
        var modPrefix = GetRootNamespace(registeredType);
        if (string.IsNullOrEmpty(modPrefix))
            return null;

        var category = GetCategoryName(attributeType);
        if (string.IsNullOrEmpty(category))
            return null;

        var structName = modPrefix + category + IdsStructSuffix;
        return modPrefix + IdExtensionsNamespaceSuffix + structName;
    }

    private static string GetRootNamespace(ITypeElement typeElement)
    {
        var ns = typeElement.GetContainingNamespace().QualifiedName;
        if (string.IsNullOrEmpty(ns))
            return string.Empty;
        var dot = ns.IndexOf('.');
        return dot < 0 ? ns : ns.Substring(0, dot);
    }

    private static string GetCategoryName(ITypeElement attributeType)
    {
        // Category is the PascalCase form of the registry's declared Identifier
        // ([Registry(Identifier = "ecs_system_group")] -> EcsSystemGroup). The registry CLR
        // short-name is NOT reliable (SystemGroupRegistry -> "ecs_system_group",
        // ModuleRegistry -> "state_module"). Must match the generated {Mod}{Category}IDs struct so
        // a reverse-index key (FromAttribute) and a lookup key (FromLeafProperty) agree.
        var registry = attributeType.GetContainingType();
        if (registry == null)
            return string.Empty;

        var identifier = GetRegistryIdentifier(registry);
        return string.IsNullOrEmpty(identifier) ? string.Empty : SnakeToPascal(identifier!);
    }

    private static string? GetRegistryIdentifier(ITypeElement registry)
    {
        var instances = registry.GetAttributeInstances(
            new ClrTypeName(RegistryAttributeFullName), AttributesSource.Self);
        foreach (var instance in instances)
        {
            var value = instance.NamedParameter("Identifier");
            if (!value.IsBadValue && value.IsConstant && value.ConstantValue.IsString())
                return value.ConstantValue.AsString();
        }

        return null;
    }

    /// <summary>
    /// Reduces a registry CLR name (<c>Sparkitect.Graphics.Vulkan.ShaderModuleRegistry</c>) to its
    /// category (<c>ShaderModule</c>): the last dotted segment minus the <c>Registry</c> suffix.
    /// </summary>
    private static string CategoryFromRegistryClrName(string registryClrName)
    {
        var lastDot = registryClrName.LastIndexOf('.');
        var shortName = lastDot < 0 ? registryClrName : registryClrName.Substring(lastDot + 1);
        return shortName.EndsWith(RegistrySuffix)
            ? shortName.Substring(0, shortName.Length - RegistrySuffix.Length)
            : shortName;
    }

    private static string SnakeToPascal(string snake)
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
