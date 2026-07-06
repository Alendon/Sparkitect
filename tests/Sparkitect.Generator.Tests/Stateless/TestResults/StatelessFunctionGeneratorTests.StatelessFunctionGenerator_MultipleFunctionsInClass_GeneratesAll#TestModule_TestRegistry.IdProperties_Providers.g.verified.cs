//HintName: TestModule_TestRegistry.IdProperties_Providers.g.cs
#nullable enable
#pragma warning disable CS9113
#pragma warning disable CS1591

using Sparkitect.Utils;

namespace TestMod.Generated.IdExtensions;

public readonly partial struct TestModTestIDs
{
private static global::Sparkitect.Modding.Identification _init_Providers;
    [global::Sparkitect.Modding.RegisteredFrom(typeof(global::TestMod.TestModule), Member = "Initialize")]
    public global::Sparkitect.Modding.Identification Init
    {
        get
        {
            var value = _init_Providers;
            if (value == default)
                new global::System.InvalidOperationException(
                    "Identification 'Init' accessed after teardown or before registration. " +
                    "Category: test, Mod: test_mod, Entry: init").Throw();
            return value;
        }
    }

    private static void Register_Init_Providers(
        global::StatelessTest.TestRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager,
        global::Sparkitect.DI.Resolution.IResolutionScope scope)
    {
        _init_Providers = identificationManager.RegisterObject("test_mod", "test", "init");
registry.Register<global::TestMod.TestModule.InitFunc>(_init_Providers);
    }

    private static void Unregister_Init_Providers(
        global::StatelessTest.TestRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager)
    {
        var id = _init_Providers;
        if (id == default) return;
registry.Unregister(id);
        identificationManager.UnregisterObject(id);
        _init_Providers = default;
    }
private static global::Sparkitect.Modding.Identification _update_Providers;
    [global::Sparkitect.Modding.RegisteredFrom(typeof(global::TestMod.TestModule), Member = "Update")]
    public global::Sparkitect.Modding.Identification Update
    {
        get
        {
            var value = _update_Providers;
            if (value == default)
                new global::System.InvalidOperationException(
                    "Identification 'Update' accessed after teardown or before registration. " +
                    "Category: test, Mod: test_mod, Entry: update").Throw();
            return value;
        }
    }

    private static void Register_Update_Providers(
        global::StatelessTest.TestRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager,
        global::Sparkitect.DI.Resolution.IResolutionScope scope)
    {
        _update_Providers = identificationManager.RegisterObject("test_mod", "test", "update");
registry.Register<global::TestMod.TestModule.UpdateFunc>(_update_Providers);
    }

    private static void Unregister_Update_Providers(
        global::StatelessTest.TestRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager)
    {
        var id = _update_Providers;
        if (id == default) return;
registry.Unregister(id);
        identificationManager.UnregisterObject(id);
        _update_Providers = default;
    }
}
