using Sparkitect.DI.GeneratorAttributes;

namespace Sparkitect.Modding;

[CreateServiceFactory<IResourceManager>]
internal class ResourceManager : IResourceManager
{
    public void SetResource(Identification objectId, string key, string name)
    {
        // Dummy implementation - will be implemented later
    }
}