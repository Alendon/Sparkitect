using System;
using System.Linq;
using System.Text;

namespace Sparkitect.Generator.Modding;

public abstract record RegistrationEntry(
    string Id,
    ImmutableValueArray<(string fileId, string fileName)> Files)
{
    public abstract string EmitRegistrationEntryCode(string registrySymbol, string idSymbol);
}

public sealed record MethodRegistrationEntry(
    string Id,
    ImmutableValueArray<(string fileId, string fileName)> Files,
    string MethodName,
    string ProviderFullName,
    ImmutableValueArray<(string paramType, bool isNullable)> DiParameters)
    : RegistrationEntry(Id, Files)
{
    public override string EmitRegistrationEntryCode(string registry, string id)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < DiParameters.Count; i++)
        {
            var (paramType, isNullable) = DiParameters[i];
            var globalType = paramType.StartsWith("global::") ? paramType : $"global::{paramType}";
            if (isNullable)
                sb.AppendLine($"Container.TryResolve<{globalType}>(out var arg_{i});");
            else
            {
                sb.AppendLine($"if(!Container.TryResolve<{globalType}>(out var arg_{i}))");
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
    string ProviderFullName)
    : RegistrationEntry(Id, Files)
{
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
    public override string EmitRegistrationEntryCode(string registry, string id)
        => $"{registry}.{MethodName}<{TypeFullName}>({id});";   // PRESERVED — D-01
}

/// <summary>
/// Concrete-type kind carried on <see cref="TypeRegistrationEntry"/> so that
/// auto-emit (49.3) can produce a matching <c>partial class</c> or <c>partial struct</c>
/// extension. Defaults to <see cref="Class"/> for backward compatibility with callers
/// that have not yet been wired through the kind-aware extraction path.
/// </summary>
public enum RegistrationTypeKind
{
    Class,
    Struct
}

public sealed record KeyedFactoryGenerationInfo(
    string TBaseFullName,
    string ConfiguratorClassName);

public sealed record ResourceRegistrationEntry(
    string Id,
    ImmutableValueArray<(string fileId, string fileName)> Files,
    string MethodName)
    : RegistrationEntry(Id, Files)
{
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
