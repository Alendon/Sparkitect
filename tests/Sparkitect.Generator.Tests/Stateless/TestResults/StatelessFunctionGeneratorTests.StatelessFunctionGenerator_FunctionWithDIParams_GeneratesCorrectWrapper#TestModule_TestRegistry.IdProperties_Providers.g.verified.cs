//HintName: TestModule_TestRegistry.IdProperties_Providers.g.cs
#nullable enable
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace TestMod.Generated.IdExtensions;

public readonly partial struct TestModTestIDs
{
private static global::Sparkitect.Modding.Identification _process_Providers;
    [global::Sparkitect.Modding.RegisteredFrom(typeof(global::TestMod.TestModule), Member = "Process")]
    public global::Sparkitect.Modding.Identification Process => _process_Providers;

    private static void Register_Process_Providers(
        global::StatelessTest.TestRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager)
    {
        _process_Providers = identificationManager.RegisterObject("test_mod", "test", "process");
registry.Register<global::TestMod.TestModule.ProcessFunc>(_process_Providers);
    }
}
