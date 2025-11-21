using MinimalSampleMod.CompilerGenerated.IdExtensions;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace MinimalSampleMod;


// define the string identifier for this registry (/category)
[Registry(Identifier = "dummy")]
// partial class to allow source gen extensions.
// The registry is Di instantiated
public partial class DummyRegistry(IDummyValueManager dummyValueManager) : IRegistry
{
    // Define a registry method. Check the SG implementation to determine the possible registry method types
    // This is a method/property registry
    [RegistryMethod]
    public void RegisterValue(Identification id, string value)
    {
        dummyValueManager.AddDummyValue(id, value);
    }

    public static string Identifier => "dummy";
    
    public void Unregister(Identification id)
    {
        // Sample implementation placeholder
    }
}