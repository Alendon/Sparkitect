using Sparkitect.Generator.Modding;

namespace Sparkitect.Generator.Stateless;

/// <summary>
/// Registration entry for stateless functions. Emits type-based registration.
/// </summary>
public sealed record StatelessRegistrationEntry(
    string Id,
    ImmutableValueArray<(string fileId, string fileName)> Files,
    string WrapperTypeFullName)
    : RegistrationEntry(Id, Files)
{
    public override string EmitRegistrationEntryCode(string registry, string id)
        => $"{registry}.Register<{WrapperTypeFullName}>({id});";
}
