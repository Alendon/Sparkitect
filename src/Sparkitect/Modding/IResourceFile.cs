namespace Sparkitect.Modding;

/// <summary>
/// Defines the contract for resource files loaded from mod archives.
/// Implementations are DI instantiated and receive their stream via <see cref="SetResourceStream"/>.
/// </summary>
public interface IResourceFile
{
    /// <summary>
    /// Called by <see cref="IResourceManager"/> to inject the resource stream after instantiation.
    /// </summary>
    /// <param name="stream">The stream containing the resource data from the mod archive.</param>
    void SetResourceStream(Stream stream);
}