using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using Sparkitect.RiderPlugin.Registrations;

namespace Sparkitect.RiderPlugin.References;

/// <summary>
/// Produces a <see cref="RegistrationIdReference" /> on the ID string-literal argument of any
/// registration attribute. Category / marker / mod detection is delegated to the shared
/// <see cref="RegistrationFactory" /> (single source of truth); the navigation target is taken from the
/// resolved <see cref="RegistrationKey" /> and the resolve/F12 behaviour stays unchanged.
/// </summary>
public class RegistrationIdReferenceFactory : IReferenceFactory
{
    public ReferenceCollection GetReferences(ITreeNode element, ReferenceCollection oldReferences)
    {
        if (element is not ICSharpLiteralExpression literal)
            return ReferenceCollection.Empty;

        if (!literal.IsConstantValue() || !literal.ConstantValue.IsString())
            return ReferenceCollection.Empty;

        var idString = literal.ConstantValue.AsString();
        if (string.IsNullOrEmpty(idString))
            return ReferenceCollection.Empty;

        var registration = RegistrationFactory.FromCSharpLiteral(literal, idString!);
        if (registration == null)
            return ReferenceCollection.Empty;

        var key = registration.Key;
        var reference = new RegistrationIdReference(
            literal, literal.GetPsiModule(), key.IdsStructClrName, key.MemberName);

        var result = new ReferenceCollection(reference);
        return ResolveUtil.ReferenceSetsAreEqual(result, oldReferences) ? oldReferences : result;
    }

    public bool HasReference(ITreeNode element, IReferenceNameContainer names)
    {
        if (element is not ICSharpLiteralExpression literal)
            return false;
        if (!literal.IsConstantValue() || !literal.ConstantValue.IsString())
            return false;
        var argument = CSharpArgumentNavigator.GetByValue(literal);
        if (argument == null)
            return false;
        return AttributeNavigator.GetByArgument(argument) != null;
    }
}
