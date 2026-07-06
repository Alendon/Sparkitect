//HintName: TestModule_TestRegistry.IdProperties_Providers.g.cs
#nullable enable
#pragma warning disable CS9113
#pragma warning disable CS1591

using Sparkitect.Utils;

namespace TestMod.Generated.IdExtensions;

public readonly partial struct TestModTestIDs
{
private static global::Sparkitect.Modding.Identification _process_Providers;
    [global::Sparkitect.Modding.RegisteredFrom(typeof(global::TestMod.TestModule), Member = "Process")]
    public global::Sparkitect.Modding.Identification Process
    {
        get
        {
            var value = _process_Providers;
            if (value == default)
                new global::System.InvalidOperationException(
                    "Identification 'Process' accessed after teardown or before registration. " +
                    "Category: test, Mod: test_mod, Entry: process").Throw();
            return value;
        }
    }

    private static void Register_Process_Providers(
        global::StatelessTest.TestRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager,
        global::Sparkitect.DI.Resolution.IResolutionScope scope)
    {
        _process_Providers = identificationManager.RegisterObject("test_mod", "test", "process");
registry.Register<global::TestMod.TestModule.ProcessFunc>(_process_Providers);
    }

    private static void Unregister_Process_Providers(
        global::StatelessTest.TestRegistry registry,
        global::Sparkitect.Modding.IIdentificationManager identificationManager,
        global::Sparkitect.Modding.IResourceManager? resourceManager)
    {
        var id = _process_Providers;
        if (id == default) return;
registry.Unregister(id);
        identificationManager.UnregisterObject(id);
        _process_Providers = default;
    }
}
