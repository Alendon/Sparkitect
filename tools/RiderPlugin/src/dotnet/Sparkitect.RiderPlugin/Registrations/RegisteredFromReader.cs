using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;

namespace Sparkitect.RiderPlugin.Registrations;

/// <summary>
/// Reads the <c>[RegisteredFrom]</c> coordinate off a generated leaf id property. This is the single
/// reliable owner edge — the originating <c>typeof(...)</c> (with optional <c>Member</c>) for source
/// registrations, or the plain <c>SourcePath</c>/<c>SourceLine</c>/<c>SourceColumn</c> for resource files.
/// The registered <c>*Func</c> wrapper type is never read; the attribute alone supplies the owner.
/// </summary>
public static class RegisteredFromReader
{
    private const string RegisteredFromFullName = "Sparkitect.Modding.RegisteredFromAttribute";

    /// <summary>The <c>[RegisteredFrom]</c> owner on <paramref name="leaf" />, or null when absent/unreadable.</summary>
    public static RegistrationOwner? Read(IProperty leaf)
    {
        var instances = leaf.GetAttributeInstances(
            new ClrTypeName(RegisteredFromFullName), AttributesSource.Self);

        foreach (var instance in instances)
        {
            var typeValue = instance.PositionParameter(0);
            if (typeValue is { IsBadValue: false, IsType: true }
                && (typeValue.TypeValue as IDeclaredType)?.GetTypeElement() is { } typeElement)
            {
                var member = ReadString(instance, "Member");
                return RegistrationOwner.ForType(typeElement, member);
            }

            var sourcePath = ReadString(instance, "SourcePath");
            if (!string.IsNullOrEmpty(sourcePath))
            {
                return RegistrationOwner.ForResource(
                    sourcePath!, ReadInt(instance, "SourceLine"), ReadInt(instance, "SourceColumn"));
            }
        }

        return null;
    }

    private static string? ReadString(IAttributeInstance instance, string name)
    {
        var value = instance.NamedParameter(name);
        return value is { IsBadValue: false, IsConstant: true }
               && value.ConstantValue.IsString()
            ? value.ConstantValue.AsString()
            : null;
    }

    private static int ReadInt(IAttributeInstance instance, string name)
    {
        var value = instance.NamedParameter(name);
        return value is { IsBadValue: false, IsConstant: true } && value.ConstantValue.IsInteger()
            ? value.ConstantValue.IntValue
            : 0;
    }
}
