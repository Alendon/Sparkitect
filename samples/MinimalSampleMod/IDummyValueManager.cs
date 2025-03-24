using Sparkitect.Modding;

namespace MinimalSampleMod;

public interface IDummyValueManager
{
    void AddDummyValue(Identification id, string value);
    string GetDummyValue(Identification id);
}