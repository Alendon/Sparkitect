using System.IO.Compression;

namespace Sparkitect.Modding;

/// <summary>
/// Manages resource loading from mod archives.
/// </summary>
public interface IResourceManager
{
    /// <summary>
    /// Associates a resource file with a registered object.
    /// </summary>
    /// <param name="objectId">The identification of the registered object.</param>
    /// <param name="key">The resource key (e.g., "module", "config").</param>
    /// <param name="name">The filename within the resource folder.</param>
    void SetResource(Identification objectId, string key, string name);

    /// <summary>
    /// Opens a stream to a resource file from the mod archive.
    /// </summary>
    /// <param name="objectId">The identification of the registered object.</param>
    /// <param name="key">The resource key.</param>
    /// <returns>A stream to the resource, or null if not found.</returns>
    Stream? GetResourceStream(Identification objectId, string key);
    
    internal void RegisterResourceFolder(string registryIdentifier, string folder);
    internal void OnModLoaded(string modId, ZipArchive? archive);
    internal void OnModUnloaded(string modId);
}