using System.Collections.Concurrent;
using JetBrains.Application.Parts;
using JetBrains.DocumentModel;
using JetBrains.ProjectModel;
using JetBrains.Util;
using Sparkitect.RiderPlugin.References;

namespace Sparkitect.RiderPlugin.Navigation;

/// <summary>
/// Reverse index mapping a registration key (the generated IDs-struct CLR name plus the leaf member
/// name) to the user-source registration sites that produce it. Populated by
/// <see cref="RegistrationSiteIndexBuilder" /> from C# attribute usages; YAML entries are added later
/// through <see cref="AddYamlEntries" />. The index is never built by parsing generated registration
/// entrypoints — it keys only off durable user-source contracts.
/// </summary>
[SolutionComponent(Instantiation.DemandAnyThreadSafe)]
public sealed class RegistrationSiteIndex
{
    private readonly ConcurrentDictionary<(string, string), LocalList<DocumentRange>> _map = new();
    private volatile bool _dirty = true;

    /// <summary>True when the index needs a rebuild before the next lookup.</summary>
    public bool IsDirty => _dirty;

    /// <summary>Marks the index stale so the next reverse-nav request rebuilds it.</summary>
    public void MarkDirty() => _dirty = true;

    /// <summary>Returns the registration-site ranges for a key, or an empty list when none are known.</summary>
    public LocalList<DocumentRange> TryGet(string idsStructClrName, string memberName)
    {
        return _map.TryGetValue((idsStructClrName, memberName), out var ranges)
            ? ranges
            : new LocalList<DocumentRange>();
    }

    /// <summary>Convenience overload taking the shared <see cref="RegistrationKey" />.</summary>
    public LocalList<DocumentRange> TryGet(RegistrationKey key) => TryGet(key.IdsStructClrName, key.MemberName);

    /// <summary>
    /// Replaces the entire C#-derived contents with a freshly built map and clears the dirty flag.
    /// Called by the builder after a full walk of user-source attribute usages.
    /// </summary>
    public void Replace(ConcurrentDictionary<(string, string), LocalList<DocumentRange>> built)
    {
        _map.Clear();
        foreach (var pair in built)
            _map[pair.Key] = pair.Value;
        _dirty = false;
    }

    /// <summary>
    /// Extension seam consumed by the YAML plan: appends YAML-sourced registration-site ranges to the
    /// same index under the same <see cref="RegistrationKey" /> contract, so reverse nav surfaces both
    /// C# and YAML sites for one id.
    /// </summary>
    public void AddYamlEntries(RegistrationKey key, LocalList<DocumentRange> yamlRanges)
    {
        _map.AddOrUpdate(
            key.AsTuple(),
            _ => yamlRanges,
            (_, existing) =>
            {
                foreach (var range in yamlRanges)
                    existing.Add(range);
                return existing;
            });
    }
}
