using Sparkitect.Generator.Modding;

namespace Sparkitect.Generator.Stateless;

/// <summary>
/// Registration entry for stateless functions. Emits type-based registration.
/// </summary>
public sealed record StatelessRegistrationEntry(
    string Id,
    ImmutableValueArray<(string fileId, string fileName)> Files,
    string WrapperTypeFullName,
    string? ParentTypeFullName = null,
    string? UserMethodName = null)
    : RegistrationEntry(Id, Files)
{
    // Pitfall 2: the backward coordinate must point at the USER method and its containing type,
    // NOT the generated *Func wrapper (which navigates to a useless generated stub).
    public override string? RegisteredTypeFullName => ParentTypeFullName;
    public override string? RegisteredMember => UserMethodName;

    public override string EmitRegistrationEntryCode(string registry, string id)
        => $"{registry}.Register<{WrapperTypeFullName}>({id});";
}
