using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;

namespace Sparkitect.RiderPlugin.References;

/// <summary>
/// Produces a <see cref="RegistrationIdReference" /> on the ID string-literal argument of any
/// registration attribute (an attribute type that carries the forward <c>RegistrationMarkerAttribute</c>).
/// The navigation target is reconstructed from reliable inputs — the marker's category argument plus the
/// owning project's csproj <c>&lt;ModId&gt;</c> — via the shared <see cref="RegistrationKey" />.
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

        var argument = CSharpArgumentNavigator.GetByValue(literal);
        if (argument == null)
            return ReferenceCollection.Empty;

        var attribute = AttributeNavigator.GetByArgument(argument);
        if (attribute == null)
            return ReferenceCollection.Empty;

        var attributeType = attribute.TypeReference?.Resolve().DeclaredElement as ITypeElement;
        if (!RegistrationMarkerPredicate.IsRegistrationAttribute(attributeType))
            return ReferenceCollection.Empty;

        var registeredType = attribute.GetContainingTypeElement(false);
        if (registeredType == null)
            return ReferenceCollection.Empty;

        var modId = SparkitectModId.Resolve(registeredType);
        if (string.IsNullOrEmpty(modId))
            return ReferenceCollection.Empty;

        var key = RegistrationKey.FromAttribute(attributeType!, idString!, modId!);
        if (key == null)
            return ReferenceCollection.Empty;

        var module = ((IClrDeclaredElement)registeredType).Module;
        var reference = new RegistrationIdReference(
            literal, module, key.Value.IdsStructClrName, key.Value.MemberName);

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
