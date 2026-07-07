//HintName: TypedRegistry.IdProperties_Providers.g.cs
#nullable enable
#pragma warning disable CS9113
#pragma warning disable CS1591

using Sparkitect.Utils;

namespace SampleTest.Generated.IdExtensions;

public readonly partial struct SampleTestTypedRegistryIDs
{
private static global::Sparkitect.Modding.Identification _typedItem_Providers;
    [global::Sparkitect.Modding.RegisteredFrom(typeof(global::DiTest.TypedItem))]
    public global::Sparkitect.Modding.Identification<global::DiTest.TypedItem> TypedItem
    {
        get
        {
            var value = _typedItem_Providers;
            if (value == default)
                new global::System.InvalidOperationException(
                    "Identification 'TypedItem' accessed after teardown or before registration. " +
                    "Category: typed_registry, Mod: sample_test, Entry: typed_item").Throw();
            return new global::Sparkitect.Modding.Identification<global::DiTest.TypedItem>(value);
        }
    }

    // Non-throwing peek: the raw id, or default when unregistered/torn down.
    internal global::Sparkitect.Modding.Identification<global::DiTest.TypedItem> TypedItemOrDefault => new global::Sparkitect.Modding.Identification<global::DiTest.TypedItem>(_typedItem_Providers);

    private static void Register_TypedItem_Providers(
        global::DiTest.TypedRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager,
        global::Sparkitect.DI.Resolution.IResolutionScope scope)
    {
        _typedItem_Providers = identificationManager.RegisterObject("sample_test", "typed_registry", "typed_item");
registry.RegisterTyped<global::DiTest.TypedItem>(_typedItem_Providers);
    }

    private static void Unregister_TypedItem_Providers(
        global::DiTest.TypedRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager)
    {
        var id = _typedItem_Providers;
        if (id == default) return;
registry.Unregister(id);
        identificationManager.UnregisterObject(id);
        _typedItem_Providers = default;
    }
private static global::Sparkitect.Modding.Identification _untypedItem_Providers;
    [global::Sparkitect.Modding.RegisteredFrom(typeof(global::DiTest.UntypedItem))]
    public global::Sparkitect.Modding.Identification UntypedItem
    {
        get
        {
            var value = _untypedItem_Providers;
            if (value == default)
                new global::System.InvalidOperationException(
                    "Identification 'UntypedItem' accessed after teardown or before registration. " +
                    "Category: typed_registry, Mod: sample_test, Entry: untyped_item").Throw();
            return value;
        }
    }

    // Non-throwing peek: the raw id, or default when unregistered/torn down.
    internal global::Sparkitect.Modding.Identification UntypedItemOrDefault => _untypedItem_Providers;

    private static void Register_UntypedItem_Providers(
        global::DiTest.TypedRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager,
        global::Sparkitect.DI.Resolution.IResolutionScope scope)
    {
        _untypedItem_Providers = identificationManager.RegisterObject("sample_test", "typed_registry", "untyped_item");
registry.RegisterUntyped<global::DiTest.UntypedItem>(_untypedItem_Providers);
    }

    private static void Unregister_UntypedItem_Providers(
        global::DiTest.TypedRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager)
    {
        var id = _untypedItem_Providers;
        if (id == default) return;
registry.Unregister(id);
        identificationManager.UnregisterObject(id);
        _untypedItem_Providers = default;
    }
}
