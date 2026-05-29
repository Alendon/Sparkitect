using System.Linq;
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
/// Resolves a <c>.sparkres.yaml</c> top-level key (<c>{registry-FQN}.{method}</c>) to the registry
/// method's declared element, so native navigation lands on the registry method. Rename is not offered.
/// </summary>
public class SparkresRegistryMethodReference : CheckedReferenceBase<IPlainScalarNode>
{
    private readonly string myRegistryClrName;
    private readonly string myMethodName;

    public SparkresRegistryMethodReference([NotNull] IPlainScalarNode owner, string fqn)
        : base(owner)
    {
        SplitFqn(fqn, out myRegistryClrName, out myMethodName);
    }

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
        var result = CheckedReferenceImplUtil.Resolve(this, GetReferenceSymbolTable(true));
        return result.Result.IsEmpty ? ResolveResultWithInfo.Unresolved : result;
    }

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
    {
        if (string.IsNullOrEmpty(myRegistryClrName) || string.IsNullOrEmpty(myMethodName))
            return EmptySymbolTable.INSTANCE;

        var module = myOwner.GetPsiModule();
        var scope = module.GetPsiServices().Symbols
            .GetSymbolScope(module, withReferences: true, caseSensitive: true);
        var registryType = scope.GetTypeElementByCLRName(new ClrTypeName(myRegistryClrName));
        if (registryType == null)
            return EmptySymbolTable.INSTANCE;

        var table = ResolveUtil.GetSymbolTableByTypeElement(registryType, SymbolTableMode.FULL, module);
        if (!useReferenceName)
            return table;

        return table.Filter(myMethodName, new ExactNameFilter(myMethodName));
    }

    public override ISymbolFilter[] GetSymbolFilters() => EmptyArray<ISymbolFilter>.Instance;

    public override string GetName() => myMethodName;

    public override TreeTextRange GetTreeTextRange() => myOwner.GetTreeTextRange();

    public override IReference BindTo(IDeclaredElement element) => this;

    public override IReference BindTo(IDeclaredElement element, ISubstitution substitution) => this;

    public override IAccessContext GetAccessContext() => new DefaultAccessContext(myOwner);

    private static void SplitFqn(string fqn, out string registryClrName, out string methodName)
    {
        var lastDot = fqn.LastIndexOf('.');
        if (lastDot <= 0 || lastDot == fqn.Length - 1)
        {
            registryClrName = string.Empty;
            methodName = string.Empty;
            return;
        }

        registryClrName = fqn.Substring(0, lastDot);
        methodName = fqn.Substring(lastDot + 1);
    }
}
