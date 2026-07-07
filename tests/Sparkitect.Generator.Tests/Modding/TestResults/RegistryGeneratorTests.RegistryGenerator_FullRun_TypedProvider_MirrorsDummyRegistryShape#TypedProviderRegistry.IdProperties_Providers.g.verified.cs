//HintName: TypedProviderRegistry.IdProperties_Providers.g.cs
#nullable enable
#pragma warning disable CS9113
#pragma warning disable CS1591

using Sparkitect.Utils;

namespace SampleTest.Generated.IdExtensions;

public readonly partial struct SampleTestTypedProviderRegistryIDs
{
private static global::Sparkitect.Modding.Identification _typedDummyProvider_Providers;
    [global::Sparkitect.Modding.RegisteredFrom(typeof(global::DiTest.TypedDummyProvider))]
    public global::Sparkitect.Modding.Identification<global::DiTest.TypedDummyProvider> TypedDummyProvider
    {
        get
        {
            var value = _typedDummyProvider_Providers;
            if (value == default)
                new global::System.InvalidOperationException(
                    "Identification 'TypedDummyProvider' accessed after teardown or before registration. " +
                    "Category: typed_provider_registry, Mod: sample_test, Entry: typed_dummy_provider").Throw();
            return new global::Sparkitect.Modding.Identification<global::DiTest.TypedDummyProvider>(value);
        }
    }

    // Non-throwing peek: the raw id, or default when unregistered/torn down.
    internal global::Sparkitect.Modding.Identification<global::DiTest.TypedDummyProvider> TypedDummyProviderOrDefault => new global::Sparkitect.Modding.Identification<global::DiTest.TypedDummyProvider>(_typedDummyProvider_Providers);

    private static void Register_TypedDummyProvider_Providers(
        global::DiTest.TypedProviderRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager,
        global::Sparkitect.DI.Resolution.IResolutionScope scope)
    {
        _typedDummyProvider_Providers = identificationManager.RegisterObject("sample_test", "typed_provider_registry", "typed_dummy_provider");
registry.RegisterTypedProvider<global::DiTest.TypedDummyProvider>(_typedDummyProvider_Providers);
    }

    private static void Unregister_TypedDummyProvider_Providers(
        global::DiTest.TypedProviderRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager)
    {
        var id = _typedDummyProvider_Providers;
        if (id == default) return;
registry.Unregister(id);
        identificationManager.UnregisterObject(id);
        _typedDummyProvider_Providers = default;
    }
}
