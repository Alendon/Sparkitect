using System;
using System.Linq;
using System.Text;

namespace Sparkitect.Generator.Modding;

public abstract record RegistrationEntry(
    string Id,
    ImmutableValueArray<(string fileId, string fileName)> Files)
{
    public abstract string EmitRegistrationEntryCode(string registrySymbol, string idSymbol);

    /// <summary>
    /// Backward-coordinate typeof target for the generated leaf ID property: the fully-qualified
    /// (global::-prefixed) type that authored this registration. Null when no C# typeof target
    /// exists yet (e.g. resource/YAML entries, whose coordinate is emitted in a later plan).
    /// Container and member are kept SEPARATE — typeof emits a TYPE, never a dotted member.
    /// </summary>
    public abstract string? RegisteredTypeFullName { get; }

    /// <summary>
    /// Optional member name accompanying <see cref="RegisteredTypeFullName"/> (the provider
    /// method/property name, or the user method name for stateless leaves). Null for the
    /// type-shape leaf, where the registered type itself IS the navigation target.
    /// </summary>
    public abstract string? RegisteredMember { get; }
}

public sealed record MethodRegistrationEntry(
    string Id,
    ImmutableValueArray<(string fileId, string fileName)> Files,
    string MethodName,
    string ProviderFullName,
    ImmutableValueArray<(string paramType, bool isNullable)> DiParameters,
    string? RegisteredContainerFullName = null,
    string? RegisteredMemberName = null)
    : RegistrationEntry(Id, Files)
{
    // typeof target is the provider's CONTAINING type (cannot typeof a member);
    // the member name is carried separately so the annotation reads typeof(Container), Member = "X".
    public override string? RegisteredTypeFullName => RegisteredContainerFullName;
    public override string? RegisteredMember => RegisteredMemberName;

    public override string EmitRegistrationEntryCode(string registry, string id)
    {
        var sb = new StringBuilder();
        // Resolve DI parameters through the registration scope (threaded in as `scope`), matching
        // every other DI-leaf code path. The provider's containing type is the wrapper-type metadata
        // key; with no registration metadata present the scope falls back to direct container resolution.
        var wrapperTypeOf = $"typeof({RegisteredContainerFullName ?? "global::System.Object"})";
        for (int i = 0; i < DiParameters.Count; i++)
        {
            var (paramType, isNullable) = DiParameters[i];
            var globalType = paramType.StartsWith("global::") ? paramType : $"global::{paramType}";
            if (isNullable)
                sb.AppendLine($"scope.TryResolve<{globalType}>({wrapperTypeOf}, out var arg_{i});");
            else
            {
                sb.AppendLine($"if(!scope.TryResolve<{globalType}>({wrapperTypeOf}, out var arg_{i}))");
                sb.AppendLine($"    throw new global::System.InvalidOperationException(\"Missing dependency {globalType} for provider {ProviderFullName}\");");
            }
        }

        var args = string.Join(", ", System.Linq.Enumerable.Range(0, DiParameters.Count).Select(i => $"arg_{i}"));
        sb.AppendLine($"var value = {ProviderFullName}({args});");
        sb.Append($"{registry}.{MethodName}({id}, value);");
        return sb.ToString();
    }
}

public sealed record PropertyRegistrationEntry(
    string Id,
    ImmutableValueArray<(string fileId, string fileName)> Files,
    string MethodName,
    string ProviderFullName,
    string? RegisteredContainerFullName = null,
    string? RegisteredMemberName = null)
    : RegistrationEntry(Id, Files)
{
    // typeof target is the provider-property's CONTAINING type; member is the property name.
    public override string? RegisteredTypeFullName => RegisteredContainerFullName;
    public override string? RegisteredMember => RegisteredMemberName;

    public override string EmitRegistrationEntryCode(string registry, string id)
        => $"var value = {ProviderFullName};\n{registry}.{MethodName}({id}, value);";
}

public sealed record TypeRegistrationEntry(
    string Id,
    ImmutableValueArray<(string fileId, string fileName)> Files,
    string MethodName,
    string TypeFullName,
    KeyedFactoryGenerationInfo? KeyedFactoryGeneration = null,
    RegistrationTypeKind TypeKind = RegistrationTypeKind.Class)
    : RegistrationEntry(Id, Files)
{
    // The registered type IS the navigation target — no member (emits a TYPE).
    public override string? RegisteredTypeFullName => TypeFullName;
    public override string? RegisteredMember => null;

    public override string EmitRegistrationEntryCode(string registry, string id)
        => $"{registry}.{MethodName}<{TypeFullName}>({id});";
}

/// <summary>
/// Concrete-type kind carried on <see cref="TypeRegistrationEntry"/> so that
/// auto-emit can produce a matching <c>partial class</c>, <c>partial struct</c>,
/// <c>partial record</c>, or <c>partial record struct</c> extension. The emitted partial's
/// kind keyword must match the registered type's source declaration, or the partials will not
/// merge and the auto-emitted <c>IHasIdentification</c> member is dropped. Defaults to
/// <see cref="Class"/> for backward compatibility with callers that have not yet been wired
/// through the kind-aware extraction path.
/// </summary>
public enum RegistrationTypeKind
{
    Class,
    Struct,
    Record,
    RecordStruct
}

public sealed record KeyedFactoryGenerationInfo(
    string TBaseFullName,
    string ConfiguratorClassName);

public sealed record ResourceRegistrationEntry(
    string Id,
    ImmutableValueArray<(string fileId, string fileName)> Files,
    string MethodName,
    string? SourcePath = null,
    int SourceLine = 0,
    int SourceColumn = 0)
    : RegistrationEntry(Id, Files)
{
    // YAML-backed leaves have no C# typeof target — the backward coordinate is a PLAIN
    // path + line/column, carried below and emitted by the template's YAML branch.
    // These two stay null so the C# typeof branch of the conditional never fires for YAML.
    public override string? RegisteredTypeFullName => null;
    public override string? RegisteredMember => null;

    public override string EmitRegistrationEntryCode(string registry, string id)
        => string.IsNullOrEmpty(MethodName) ? "" : $"{registry}.{MethodName}({id});";
}

public sealed record RegistrationUnit(
    RegistryModel Model,
    SourceKind SourceKind,
    string SourceTag,
    ImmutableValueArray<RegistrationEntry> Entries);

public enum SourceKind
{
    Provider,
    Yaml
}
