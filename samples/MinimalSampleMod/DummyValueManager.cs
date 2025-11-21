using Serilog;
using Serilog.Core;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.GameState;
using Sparkitect.Modding;

namespace MinimalSampleMod;

[StateService<IDummyValueManager, SampleModule>]
public class DummyValueManager : IDummyValueManager
{
    private readonly Dictionary<Identification, string> _values = [];
    
    public void AddDummyValue(Identification id, string value)
    {
        Log.Information("Registering value '{Value}' for '{Id}'", value, id);
        _values[id] = value;
    }

    public void RemoveDummyValue(Identification id)
    {
        _values.Remove(id);
    }

    public string GetDummyValue(Identification id)
    {
        return _values[id];
    }
}