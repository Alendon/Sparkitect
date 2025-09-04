using System;

namespace Sparkitect.Generator.Modding;

public readonly record struct RegistrationEntry(
    string Id,
    EntryKind Kind,
    string MethodName,
    string ProviderContainingType,
    string ProviderMemberName,
    ImmutableValueArray<(string fileId, string fileName)> Files,
    ImmutableValueArray<(string paramType, bool isNullable)> DiParameters);

public enum EntryKind
{
    Method,
    Type,
    Resource,
    Property
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
