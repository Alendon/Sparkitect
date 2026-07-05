using System.Linq;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;

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
                var member = ReadString(instance, "Member") ?? ReadMemberSyntactically(leaf);
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

    /// <summary>
    /// Lexical fallback for the <c>Member</c> named argument: reads the string literal token straight
    /// from the attribute syntax on the leaf's declaration, bypassing the constant evaluator entirely
    /// (see the <see cref="ReadString" /> degrade note).
    /// </summary>
    private static string? ReadMemberSyntactically(IProperty leaf)
    {
        foreach (var declaration in leaf.GetDeclarations())
        {
            if (declaration is not IAttributesOwnerDeclaration attributesOwner)
                continue;

            foreach (var attribute in attributesOwner.AttributesEnumerable)
            {
                if (attribute.Name?.ShortName is not ("RegisteredFrom" or "RegisteredFromAttribute"))
                    continue;

                foreach (var assignment in attribute.PropertyAssignments)
                {
                    if (assignment.PropertyNameIdentifier?.Name != "Member")
                        continue;

                    var text = (assignment.Source as ICSharpLiteralExpression)?.Literal?.GetText();
                    if (text is { Length: > 2 } && text[0] == '"' && text[text.Length - 1] == '"')
                        return text.Substring(1, text.Length - 2);
                }
            }
        }

        return null;
    }

    private static string? ReadString(IAttributeInstance instance, string name)
    {
        try
        {
            var value = instance.NamedParameter(name);
            return value is { IsBadValue: false, IsConstant: true }
                   && value.ConstantValue.IsString()
                ? value.ConstantValue.AsString()
                : null;
        }
        catch (System.NullReferenceException)
        {
            // NamedParameter runs the C# constant evaluator, which can NRE inside ReSharper's
            // conversion classification (GetRuntimeFeatures) for net10.0 modules. The named argument
            // is auxiliary — degrade to absent rather than killing the caller.
            return null;
        }
    }

    private static int ReadInt(IAttributeInstance instance, string name)
    {
        try
        {
            var value = instance.NamedParameter(name);
            return value is { IsBadValue: false, IsConstant: true } && value.ConstantValue.IsInteger()
                ? value.ConstantValue.IntValue
                : 0;
        }
        catch (System.NullReferenceException)
        {
            return 0; // same evaluator NRE as ReadString — treat as absent
        }
    }
}
