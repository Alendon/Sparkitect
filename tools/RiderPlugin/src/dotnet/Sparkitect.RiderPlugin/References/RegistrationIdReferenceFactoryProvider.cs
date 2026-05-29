using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace Sparkitect.RiderPlugin.References;

/// <summary>
/// Auto-discovered factory provider that supplies <see cref="RegistrationIdReferenceFactory" />
/// for C# source files, attaching registration-ID references to the platform's reference registry.
/// </summary>
[ReferenceProviderFactory(ReferenceTypes = [typeof(RegistrationIdReference)])]
public class RegistrationIdReferenceFactoryProvider : IReferenceProviderFactory
{
    public RegistrationIdReferenceFactoryProvider(Lifetime lifetime)
    {
        Changed = new Signal<IReferenceProviderFactory>(lifetime, GetType().FullName);
    }

    public IReferenceFactory? CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex? wordIndex)
    {
        return sourceFile.PrimaryPsiLanguage.Is<CSharpLanguage>()
            ? new RegistrationIdReferenceFactory()
            : null;
    }

    public ISignal<IReferenceProviderFactory> Changed { get; }
}
