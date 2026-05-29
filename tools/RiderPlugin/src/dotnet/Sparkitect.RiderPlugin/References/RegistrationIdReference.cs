using JetBrains.Annotations;
using JetBrains.Metadata.Reader.Impl;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.ExtensionsAPI.Resolve;
using JetBrains.ReSharper.Psi.Modules;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace Sparkitect.RiderPlugin.References;

/// <summary>
/// Resolves a registration ID string literal to the generated <c>{Mod}{Category}IDs.{Entry}</c>
/// leaf property so native navigation and Find Usages follow it. Rename is intentionally not offered.
/// </summary>
public class RegistrationIdReference : CheckedReferenceBase<ICSharpLiteralExpression>
{
    private readonly IPsiModule myModule;
    private readonly string myTargetTypeFullName;
    private readonly string myMemberName;

    public RegistrationIdReference(
        [NotNull] ICSharpLiteralExpression owner,
        IPsiModule module,
        string targetTypeFullName,
        string memberName)
        : base(owner)
    {
        myModule = module;
        myTargetTypeFullName = targetTypeFullName;
        myMemberName = memberName;
    }

    public override ResolveResultWithInfo ResolveWithoutCache()
    {
        var result = CheckedReferenceImplUtil.Resolve(this, GetReferenceSymbolTable(true));
        // Extend-never-break (D-05/D-09): a registration-ID literal is just a string to the C#
        // language, so an unresolved reference MUST be a silent no-op — never a red "Cannot resolve
        // symbol" error. ResolveResultWithInfo.Ignore carries ResolveErrorType.IGNORABLE, which the
        // resolve-problem daemon stage does not highlight. Resolution still navigates when the target
        // is found; it just degrades to nothing (like vanilla F12 on a literal) when it is not.
        return result.Result.IsEmpty ? ResolveResultWithInfo.Ignore : result;
    }

    public override ISymbolTable GetReferenceSymbolTable(bool useReferenceName)
    {
        var targetType = ResolveTargetType();
        if (targetType == null)
            return EmptySymbolTable.INSTANCE;

        var table = ResolveUtil.GetSymbolTableByTypeElement(targetType, SymbolTableMode.FULL, myModule);
        if (!useReferenceName)
            return table;

        return table.Filter(myMemberName, new ExactNameFilter(myMemberName));
    }

    public override ISymbolFilter[] GetSymbolFilters() => EmptyArray<ISymbolFilter>.Instance;

    public override string GetName() => myMemberName;

    public override TreeTextRange GetTreeTextRange() => myOwner.GetStringLiteralContentTreeRange();

    public override IReference BindTo(IDeclaredElement element) => this;

    public override IReference BindTo(IDeclaredElement element, ISubstitution substitution) => this;

    public override IAccessContext GetAccessContext() => new DefaultAccessContext(myOwner);

    private ITypeElement? ResolveTargetType()
    {
        var scope = myModule.GetPsiServices().Symbols
            .GetSymbolScope(myModule, withReferences: true, caseSensitive: true);
        return scope.GetTypeElementByCLRName(new ClrTypeName(myTargetTypeFullName));
    }
}
