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
public partial class SampleEntryState : IStateDescriptor, IHasIdentification
{
    public static Identification ParentId => StateID.Sparkitect.Root;

    public static IReadOnlyList<Identification> Modules =>
    [
        StateModuleID.MinimalSampleMod.Sample,
        StateModuleID.Sparkitect.Vulkan,
        StateModuleID.Sparkitect.Ecs,
        StateModuleID.Sparkitect.RenderGraph,
        StateModuleID.Sparkitect.Windowing
    ];

    [DummyRegistry.RegisterValue("hello1")]
    public static string SomeValueToRegister() => "Hello World";

    [TransitionFunction("dummy_value_read")]
    [OnCreateScheduling]
    [OrderAfter<SampleModule.ProcessRegistryEnterFunc>]
    public static void ReadDummyValues(IDummyValueManager dummyValueManager)
    {
        Log.Information("Dummy value from method registry: {value}",dummyValueManager.GetDummyValue(DummyID.MinimalSampleMod.Hello1));
        Log.Information("Dummy value from provider type: {value}", dummyValueManager.GetDummyValue(DummyID.MinimalSampleMod.DummyProvider));
    }

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
}

[DummyRegistry.RegisterProvider("dummy_provider")]
internal partial class DummyValueProvider : IDummyValueProvider, IHasIdentification
{
    public string Provide() => "Hello from Provider";
}