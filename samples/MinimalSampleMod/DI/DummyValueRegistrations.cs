using Sparkitect.DI;
using Sparkitect.Modding;

namespace MinimalSampleMod.DI;

[RegistrationsEntrypoint]
public class DummyValueRegistrations(IIdentificationManager identificationManager) : Registrations<DummyRegistry>
{
    public override string CategoryIdentifier => "dummy_values";
    
    public override void MainPhaseRegistration(DummyRegistry dummyRegistry)
    {
        var id = identificationManager.RegisterObject("minimal_sample_mod", "dummy_values", "hello");
        dummyRegistry.RegisterDummyValue(id, "world");
    }
}