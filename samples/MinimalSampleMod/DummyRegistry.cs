using Sparkitect.Modding;
using Sparkitect.Settings;

namespace MinimalSampleMod;


// define the string identifier for this registry (/category)
[Registry(Identifier = "dummy")]
// partial class to allow source gen extensions.
// The registry is Di instantiated
public partial class DummyRegistry(IDummyValueManager dummyValueManager, ISettingsManager settingsManager) : IRegistry<SampleModule>
{
    // Define a registry method. Check the SG implementation to determine the possible registry method types
    // This is a method/property registry
    [RegistryMethod]
    public void RegisterValue<[TypedIdentification] TValue>(Identification id, TValue value)
    {
        dummyValueManager.AddDummyValue(id, value?.ToString() ?? string.Empty);
    }

    [RegistryMethod]
    [KeyedFactoryGenerationMarker<IDummyValueProvider>]
    public void RegisterProvider<[TypedIdentification] TProvider>(Identification id) where TProvider : class, IDummyValueProvider, IHasIdentification
    {
        dummyValueManager.AddDummyProvider<TProvider>(id);
    }

    // Dual-marker cross-registry method: TPayload is this registry's own bare same-registry
    // marker; TSettingValue's marker feeds an SG-emitted settings alias. The body hand-authors the
    // matching runtime fan-out — own manager AND the settings declare call — since the generator
    // never issues cross-registry calls itself.
    [RegistryMethod]
    public void RegisterTypedProvider<[TypedIdentification] TPayload, [TypedIdentification<SettingRegistry>] TSettingValue>(
        Identification id, TypedSettingProvider<TPayload, TSettingValue> provider)
        where TPayload : class, IDummyValueProvider, IHasIdentification
    {
        dummyValueManager.AddDummyProvider<TPayload>(id);
        settingsManager.Declare(new Identification<TSettingValue>(id), new SettingDefinition<TSettingValue>(provider.Value));
    }

    public static string Identifier => "dummy";

    public void Unregister(Identification id)
    {
        // Sample implementation placeholder
    }
}

public interface IDummyValueProvider
{
    string Provide();
}

// Carries both this method's payload type and the cross-registry setting value so the
// generator's value-source resolution sees both type parameters through one constructed-generic
// slot (the provider's return type closes both positionally).
public readonly record struct TypedSettingProvider<TPayload, TSettingValue>(TSettingValue Value);
