using Sparkitect.CompilerGenerated.IdExtensions;
using MinimalSampleMod.CompilerGenerated.IdExtensions;
using Serilog;
using Silk.NET.Vulkan;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.Stateless;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace MinimalSampleMod;

[StateRegistry.RegisterState("sample")]
public partial class SampleEntryState : IStateDescriptor
{
    public static Identification ParentId => StateID.Sparkitect.Root;
    public static Identification Identification => StateID.MinimalSampleMod.Sample;

    public static IReadOnlyList<Identification> Modules =>
    [
        StateModuleID.MinimalSampleMod.Sample, StateModuleID.Sparkitect.Vulkan, StateModuleID.Sparkitect.Ecs
    ];

    [DummyRegistry.RegisterValue("hello1")]
    public static string SomeValueToRegister() => "Hello World";

    [TransitionFunction("test_command_pool")]
    [OnCreateScheduling]
    [OrderAfter<VulkanModule.CreateDeviceFunc>]
    public static void TestCommandPool(IVulkanContext vulkanContext)
    {
        Log.Information("Testing VkCommandPool...");

        var poolResult = vulkanContext.CreateCommandPool(CommandPoolCreateFlags.ResetCommandBufferBit, 0);

        if (poolResult is not Result<VkCommandPool, VkApiResult>.Ok(var pool))
        {
            Log.Error("Failed to create command pool");
            return;
        }

        var singleResult = pool.AllocateCommandBuffer(CommandBufferLevel.Primary);
        if (singleResult is Result<VkCommandBuffer, VkApiResult>.Ok(var singleBuffer))
            Log.Information("Allocated single command buffer: {Handle}", singleBuffer.Handle.Handle);

        var batchResult = pool.AllocateCommandBuffers(CommandBufferLevel.Primary, 3);
        if (batchResult is Result<VkCommandBuffer[], VkApiResult>.Ok(var batchBuffers))
        {
            Log.Information("Allocated batch of {Count} command buffers", batchBuffers.Length);
            pool.FreeCommandBuffers(batchBuffers);
            Log.Information("Freed batch of command buffers");
        }

        pool.Dispose();
        Log.Information("Command pool disposed (single buffer auto-freed)");
    }

    [PerFrameFunction("print_on_frame")]
    [PerFrameScheduling]
    public static void PrintOnFrame(IDummyValueManagerStateFacade dummyValueManager)
    {
        Log.Information("Dummy Value fetched for {Id} as: {Value}", DummyID.MinimalSampleMod.Hello1,
            dummyValueManager.GetDummyFacaded(DummyID.MinimalSampleMod.Hello1));
        
        Log.Information("Dummy Value from Provider fetched for {Id} as: {Provider}", DummyID.MinimalSampleMod.DummyProvider,
            dummyValueManager.GetDummyFacaded(DummyID.MinimalSampleMod.DummyProvider));
        Thread.Sleep(1000);
    }
}

[DummyRegistry.RegisterProvider("dummy_provider")]
internal partial class DummyValueProvider : IDummyValueProvider, IHasIdentification
{
    public string Provide() => "Hello from Provider";

    public static Identification Identification => DummyID.MinimalSampleMod.DummyProvider;
}