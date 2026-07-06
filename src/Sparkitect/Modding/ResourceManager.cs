using System.IO.Compression;
using Sparkitect.GameState;
using Sparkitect.Utils.DU;

namespace Sparkitect.Modding;

[StateService<IResourceManager, CoreModule>]
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

        if (IdentificationManager.GetModId(objectId.ModId) is not Result<string, ResolveError>.Ok(var modId))
            return null;

        if (IdentificationManager.GetCategoryId(objectId.CategoryId) is not Result<string, ResolveError>.Ok(var registryId))
            return null;

        if (!_modArchives.TryGetValue(modId, out var archive) || archive is null)
            return null;

        if (!_registryFolders.TryGetValue(registryId, out var folder))
            return null;

        var entryPath = $"resources/{folder}/{filename}";
        var entry = archive.GetEntry(entryPath);

        return entry?.Open();
    }

    public void RemoveResource(Identification objectId, string key)
    {
        _resourceMappings.Remove((objectId, key));
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
        if (IdentificationManager.GetModId(modId) is not Result<ushort, ResolveError>.Ok(var numericModId))
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
