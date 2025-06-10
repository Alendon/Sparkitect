using Sparkitect.Modding;

namespace MinimalSampleMod;

[Registry(Identifier = "dummy")]
public class DummyRegistry(IDummyValueManager dummyValueManager) : IRegistry
{
    public void RegisterDummyValue(Identification id, string value)
    {
        
    }
}