using System.Collections.Concurrent;
using System.Collections.Generic;
using JetBrains.Application.Parts;
using JetBrains.DocumentModel;
using JetBrains.Lifetimes;
using JetBrains.ProjectModel;
using System.Linq;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Files;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Util;
using Sparkitect.RiderPlugin.References;

namespace Sparkitect.RiderPlugin.Navigation;

/// <summary>
/// Populates <see cref="RegistrationSiteIndex" /> from user-source C# registration attribute usages.
/// Builds lazily on the first reverse-nav request (and after the index is marked dirty); a PSI
/// change handler marks the index dirty so edits to attributes are picked up. Never parses generated
/// registration entrypoints.
/// </summary>
[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public sealed partial class RegistrationSiteIndexBuilder
{
    private readonly ISolution _solution;
    private readonly RegistrationSiteIndex _index;
    private readonly Lifetime _lifetime;
    private readonly object _buildLock = new();
    private bool _changeObserverWired;

    public RegistrationSiteIndexBuilder(Lifetime lifetime, ISolution solution, RegistrationSiteIndex index)
    {
        _lifetime = lifetime;
        _solution = solution;
        _index = index;
    }

    // The commit observer is wired lazily on first use, NOT in the constructor. PsiServicesImpl is
    // not deadlock-safe, and ReSharper forbids resolving it synchronously from a component
    // constructor (ReentrantGetValueException). By the first GetOrBuild we are well outside ctor
    // activation, so solution.GetPsiServices() is safe.
    private void EnsureChangeObserver()
    {
        if (_changeObserverWired)
            return;

        lock (_buildLock)
        {
            if (_changeObserverWired)
                return;

            // Any committed attribute add/remove/edit invalidates the C#-derived contents; the next
            // reverse-nav request rebuilds lazily.
            _solution.GetPsiServices().Files.ObserveAfterCommit(_lifetime, () => _index.MarkDirty());
            _changeObserverWired = true;
        }
    }

    /// <summary>Appends YAML resource-file registration sites via the index seam (partial Yaml file).</summary>
    partial void IndexYamlSites();

    /// <summary>Rebuilds the index from scratch if it is stale, then returns it ready for lookups.</summary>
    public RegistrationSiteIndex GetOrBuild()
    {
        EnsureChangeObserver();

        if (!_index.IsDirty)
            return _index;

        lock (_buildLock)
        {
            if (!_index.IsDirty)
                return _index;

            var built = new ConcurrentDictionary<(string, string), LocalList<DocumentRange>>();
            foreach (var sourceFile in EnumerateCSharpSourceFiles())
                IndexFile(sourceFile, built);

            _index.Replace(built);

            // YAML resource-file registration sites are appended through the index's extension seam
            // after the C# rebuild (implemented in the partial Yaml file), so reverse nav from a
            // generated id offers either the C# attribute site or the YAML entry.
            IndexYamlSites();

            return _index;
        }
    }

    private IEnumerable<IPsiSourceFile> EnumerateCSharpSourceFiles()
    {
        foreach (var project in _solution.GetAllProjects())
        foreach (var projectFile in project.GetAllProjectFiles())
        {
            var sourceFile = projectFile.ToSourceFile();
            if (sourceFile != null && sourceFile.PrimaryPsiLanguage.Is<CSharpLanguage>())
                yield return sourceFile;
        }
    }

    private static void IndexFile(
        IPsiSourceFile sourceFile,
        ConcurrentDictionary<(string, string), LocalList<DocumentRange>> built)
    {
        if (sourceFile.GetPsiFiles<CSharpLanguage>().OfType<ICSharpFile>().FirstOrDefault() is not { } file)
            return;

        foreach (var attribute in file.Descendants<IAttribute>().Collect())
            IndexAttribute(attribute, built);
    }

    private static void IndexAttribute(
        IAttribute attribute,
        ConcurrentDictionary<(string, string), LocalList<DocumentRange>> built)
    {
        var attributeType = attribute.TypeReference?.Resolve().DeclaredElement as ITypeElement;
        if (!RegistrationMarkerPredicate.IsRegistrationAttribute(attributeType))
            return;

        var registeredType = attribute.GetContainingTypeElement(false);
        if (registeredType == null)
            return;

        var modId = SparkitectModId.Resolve(registeredType);
        if (string.IsNullOrEmpty(modId))
            return;

        foreach (var argument in attribute.Arguments)
        {
            if (argument.Value is not ICSharpLiteralExpression literal)
                continue;
            if (!literal.IsConstantValue() || !literal.ConstantValue.IsString())
                continue;

            var idString = literal.ConstantValue.AsString();
            if (string.IsNullOrEmpty(idString))
                continue;

            var key = RegistrationKey.FromAttribute(attributeType!, idString!, modId!);
            if (key == null)
                continue;

            var range = literal.GetDocumentRange();
            if (!range.IsValid())
                continue;

            built.AddOrUpdate(
                key.Value.AsTuple(),
                _ => new LocalList<DocumentRange>(new[] { range }),
                (_, existing) =>
                {
                    existing.Add(range);
                    return existing;
                });

            // Only the first string-literal argument carries the id.
            break;
        }
    }
}
