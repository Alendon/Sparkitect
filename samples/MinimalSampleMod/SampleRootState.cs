using Sparkitect.CompilerGenerated.IdExtensions;
using MinimalSampleMod.CompilerGenerated.IdExtensions;
using Serilog;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace MinimalSampleMod;

[StateRegistry.RegisterState("sample")]
public partial class SampleEntryState : IStateDescriptor
{
    public static Identification ParentId => StateID.Sparkitect.Root;
    public static Identification Identification => StateID.MinimalSampleMod.Sample;
    public static IReadOnlyList<Identification> Modules => [StateModuleID.MinimalSampleMod.Sample];
    
    [DummyRegistry.RegisterValue("hello1")]
    public static string SomeValueToRegister() => "Hello World";

    [PerFrame]
    [StateFunction("print_on_frame")]
    public static void PrintOnFrame(IDummyValueManager dummyValueManager)
    {
        Log.Information("Dummy Value fetched as: {Value}", dummyValueManager.GetDummyValue(DummyID.MinimalSampleMod.Hello1));
        Thread.Sleep(1000);
    }
}