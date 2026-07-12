using JetBrains.DataFlow;
using JetBrains.Lifetimes;
using JetBrains.ReSharper.Plugins.Yaml.Psi;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.ReSharper.Psi.Resolve;
using JetBrains.ReSharper.Psi.Tree;

namespace Sparkitect.RiderPlugin.References;

/// <summary>
/// Auto-discovered factory provider that supplies <see cref="SparkresReferenceFactory" /> for
/// <c>.sparkres.yaml</c> resource files, attaching registration references to the entry-ID scalar and
/// the top-level registry-method key. Gated on the YAML language and the resource-file suffix.
/// </summary>
[ReferenceProviderFactory(ReferenceTypes = [typeof(SparkresEntryIdReference), typeof(SparkresRegistryMethodReference)])]
public class SparkresReferenceFactoryProvider : IReferenceProviderFactory
{
    private const string SparkresSuffix = ".sparkres.yaml";

    public SparkresReferenceFactoryProvider(Lifetime lifetime)
    {
        Changed = new Signal<IReferenceProviderFactory>(GetType().FullName);
    }

    public IReferenceFactory? CreateFactory(IPsiSourceFile sourceFile, IFile file, IWordIndex? wordIndex)
    {
        if (!sourceFile.PrimaryPsiLanguage.Is<YamlLanguage>())
            return null;

        return sourceFile.Name.EndsWith(SparkresSuffix)
            ? new SparkresReferenceFactory()
            : null;
    }

    public ISignal<IReferenceProviderFactory> Changed { get; }
}
