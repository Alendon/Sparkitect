using System.Linq;
using System.Text;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;

namespace Sparkitect.RiderPlugin.References;

/// <summary>
/// Produces a <see cref="RegistrationIdReference" /> on the ID string-literal argument of any
/// registration attribute (an attribute whose type implements <c>Sparkitect.Modding.IRegisterMarker</c>).
/// </summary>
public class RegistrationIdReferenceFactory : IReferenceFactory
{
    private const string IdExtensionsNamespaceSuffix = ".CompilerGenerated.IdExtensions.";
    private const string RegistryAttributeFullName = "Sparkitect.Modding.RegistryAttribute";

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

        var targetTypeFullName = BuildTargetTypeFullName(registeredType, attributeType!);
        if (targetTypeFullName == null)
            return ReferenceCollection.Empty;

        var memberName = SnakeToPascal(idString!);
        if (string.IsNullOrEmpty(memberName))
            return ReferenceCollection.Empty;

        var module = ((IClrDeclaredElement)registeredType).Module;
        var reference = new RegistrationIdReference(literal, module, targetTypeFullName, memberName);

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

    private static string? BuildTargetTypeFullName(ITypeElement registeredType, ITypeElement attributeType)
    {
        var modPrefix = GetRootNamespace(registeredType);
        if (string.IsNullOrEmpty(modPrefix))
            return null;

        var category = GetCategoryName(attributeType);
        if (string.IsNullOrEmpty(category))
            return null;

        var structName = modPrefix + category + "IDs";
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
        // The generator names the IDs struct {Mod}{Category}IDs where {Category} is the PascalCase
        // form of the registry's declared Identifier (e.g. [Registry(Identifier = "ecs_system_group")]
        // -> EcsSystemGroup -> MinimalSampleModEcsSystemGroupIDs). The registry's CLR short-name is NOT
        // a reliable source: SystemGroupRegistry -> "ecs_system_group", ModuleRegistry -> "state_module".
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
