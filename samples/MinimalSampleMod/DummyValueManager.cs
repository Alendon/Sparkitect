using Serilog;
using Serilog.Core;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.Modding;

namespace MinimalSampleMod;

[Singleton<IDummyValueManager>]
public class DummyValueManager : IDummyValueManager
{
    public void AddDummyValue(Identification id, string value)
    {
        Log.Information("Registering value '{Value}' for '{Id}'", value, id);
    }

    public string GetDummyValue(Identification id)
    {
        return "";
    }
}