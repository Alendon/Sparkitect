using System.IO.Compression;
using Sparkitect.DI.GeneratorAttributes;

namespace Sparkitect.Modding;

[CreateServiceFactory<IResourceManager>]
internal class ResourceManager : IResourceManager
{
    public required IIdentificationManager IdentificationManager { private get; init; }

    private readonly Dictionary<(Identification objectId, string key), string> _resourceMappings = new();
    private readonly Dictionary<string, string> _registryFolders = new();
    private readonly Dictionary<string, ZipArchive?> _modArchives = new();

    public void SetResource(Identification objectId, string key, string name)
    {
        _resourceMappings[(objectId, key)] = name;
    }

    public Stream? GetResourceStream(Identification objectId, string key)
    {
        if (!_resourceMappings.TryGetValue((objectId, key), out var filename))
            return null;

        if (!IdentificationManager.TryGetModId(objectId.ModId, out var modId))
            return null;

        if (!IdentificationManager.TryGetCategoryId(objectId.CategoryId, out var registryId))
            return null;

        if (!_modArchives.TryGetValue(modId, out var archive) || archive is null)
            return null;

        if (!_registryFolders.TryGetValue(registryId, out var folder))
            return null;

        var entryPath = $"resources/{folder}/{filename}";
        var entry = archive.GetEntry(entryPath);

        return entry?.Open();
    }

    public void RegisterResourceFolder(string registryIdentifier, string folder)
    {
        _registryFolders[registryIdentifier] = folder;
    }

    public void OnModLoaded(string modId, ZipArchive? archive)
    {
        _modArchives[modId] = archive;
    }

    public void OnModUnloaded(string modId)
    {
        _modArchives.Remove(modId);

        // Clean up resource mappings for this mod
        if (!IdentificationManager.TryGetModId(modId, out var numericModId))
            return;

        var keysToRemove = _resourceMappings.Keys
            .Where(k => k.objectId.ModId == numericModId)
            .ToList();

        foreach (var key in keysToRemove)
        {
            _resourceMappings.Remove(key);
        }
    }
}
