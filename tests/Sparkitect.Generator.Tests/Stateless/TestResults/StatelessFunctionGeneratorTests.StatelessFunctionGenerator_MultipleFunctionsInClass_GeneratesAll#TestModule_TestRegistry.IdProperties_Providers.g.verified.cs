//HintName: TestModule_TestRegistry.IdProperties_Providers.g.cs
#nullable enable
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace TestMod.Generated.IdExtensions;

public readonly partial struct TestModTestIDs
{
private static global::Sparkitect.Modding.Identification _init_Providers;
    [global::Sparkitect.Modding.RegisteredFrom(typeof(global::TestMod.TestModule), Member = "Initialize")]
    public global::Sparkitect.Modding.Identification Init => _init_Providers;

    private static void Register_Init_Providers(
        global::StatelessTest.TestRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager)
    {
        _init_Providers = identificationManager.RegisterObject("test_mod", "test", "init");
registry.Register<global::TestMod.TestModule.InitFunc>(_init_Providers);
    }
private static global::Sparkitect.Modding.Identification _update_Providers;
    [global::Sparkitect.Modding.RegisteredFrom(typeof(global::TestMod.TestModule), Member = "Update")]
    public global::Sparkitect.Modding.Identification Update => _update_Providers;

    private static void Register_Update_Providers(
        global::StatelessTest.TestRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager)
    {
        _update_Providers = identificationManager.RegisterObject("test_mod", "test", "update");
registry.Register<global::TestMod.TestModule.UpdateFunc>(_update_Providers);
    }
}
