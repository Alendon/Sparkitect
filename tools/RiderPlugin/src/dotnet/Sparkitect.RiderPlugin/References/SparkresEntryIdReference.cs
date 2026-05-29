using JetBrains.Annotations;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Plugins.Yaml.Psi.Tree;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace Sparkitect.RiderPlugin.References;

/// <summary>
/// Resolves a <c>.sparkres.yaml</c> entry-ID scalar to the generated <c>{Mod}{Category}IDs.{Entry}</c>
/// leaf property via the same symbol-table mechanism as the C# forward reference, so native navigation
/// and Find Usages cross the C#/YAML boundary. Rename is intentionally not offered.
/// </summary>
public class SparkresEntryIdReference : CheckedReferenceBase<IPlainScalarNode>
{
    private readonly RegistrationKey myKey;

    public SparkresEntryIdReference([NotNull] IPlainScalarNode owner, RegistrationKey key)
        : base(owner)
    {
        myKey = key;
    }

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
        var result = CheckedReferenceImplUtil.Resolve(this, GetReferenceSymbolTable(true));
        return result.Result.IsEmpty ? ResolveResultWithInfo.Unresolved : result;
    }

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
    {
        var module = myOwner.GetPsiModule();
        var targetType = ResolveTargetType(module);
        if (targetType == null)
            return EmptySymbolTable.INSTANCE;

        var table = ResolveUtil.GetSymbolTableByTypeElement(targetType, SymbolTableMode.FULL, module);
        if (!useReferenceName)
            return table;

        return table.Filter(myKey.MemberName, new ExactNameFilter(myKey.MemberName));
    }

    public override ISymbolFilter[] GetSymbolFilters() => EmptyArray<ISymbolFilter>.Instance;

    public override string GetName() => myKey.MemberName;

    public override TreeTextRange GetTreeTextRange() => myOwner.GetTreeTextRange();

    public override IReference BindTo(IDeclaredElement element) => this;

    public override IReference BindTo(IDeclaredElement element, ISubstitution substitution) => this;

    public override IAccessContext GetAccessContext() => new DefaultAccessContext(myOwner);

    private ITypeElement? ResolveTargetType(IPsiModule module)
    {
        var scope = module.GetPsiServices().Symbols
            .GetSymbolScope(module, withReferences: true, caseSensitive: true);
        return scope.GetTypeElementByCLRName(new ClrTypeName(myKey.IdsStructClrName));
    }
}
