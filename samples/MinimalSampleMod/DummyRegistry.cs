using Sparkitect.Modding;

namespace MinimalSampleMod;

public class DummyRegistry(IDummyValueManager dummyValueManager) : IRegistry
{
    public void RegisterDummyValue(Identification id, string value)
    {
        
    }
}