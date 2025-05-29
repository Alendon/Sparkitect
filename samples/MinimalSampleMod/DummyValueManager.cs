using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.Modding;

namespace MinimalSampleMod;

[Singleton<IDummyValueManager>]
public class DummyValueManager : IDummyValueManager
{
    public void AddDummyValue(Identification id, string value)
    {
        throw new NotImplementedException();
    }

    public string GetDummyValue(Identification id)
    {
        throw new NotImplementedException();
    }
}