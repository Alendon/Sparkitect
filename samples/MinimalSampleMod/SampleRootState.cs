using Sparkitect.CompilerGenerated.IdExtensions;
using MinimalSampleMod.CompilerGenerated.IdExtensions;
using Serilog;
using Silk.NET.Vulkan;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace MinimalSampleMod;

[StateRegistry.RegisterState("sample")]
public partial class SampleEntryState : IStateDescriptor
{
    public static Identification ParentId => StateID.Sparkitect.Root;
    public static Identification Identification => StateID.MinimalSampleMod.Sample;
    public static IReadOnlyList<Identification> Modules => [StateModuleID.MinimalSampleMod.Sample, StateModuleID.Sparkitect.Vulkan];
    
    [DummyRegistry.RegisterValue("hello1")]
    public static string SomeValueToRegister() => "Hello World";

    [StateFunction("test_command_pool")]
    [OnCreate]
    [OrderAfter<VulkanModule>(VulkanModule.CreateDevice_Key)]
    public static void TestCommandPool(IVulkanContext vulkanContext)
    {
        Log.Information("Testing VkCommandPool...");

        var poolResult = vulkanContext.CreateCommandPool(CommandPoolCreateFlags.ResetCommandBufferBit, 0);
        
        if (poolResult is not VkResult<VkCommandPool>.Success(var pool))
        {
            Log.Error("Failed to create command pool");
            return;
        }

        var singleResult = pool.AllocateCommandBuffer(CommandBufferLevel.Primary);
        if (singleResult is VkResult<VkCommandBuffer>.Success(var singleBuffer))
            Log.Information("Allocated single command buffer: {Handle}", singleBuffer.Handle.Handle);

        var batchResult = pool.AllocateCommandBuffers(CommandBufferLevel.Primary, 3);
        if (batchResult is VkResult<VkCommandBuffer[]>.Success(var batchBuffers))
        {
            Log.Information("Allocated batch of {Count} command buffers", batchBuffers.Length);
            pool.FreeCommandBuffers(batchBuffers);
            Log.Information("Freed batch of command buffers");
        }

        pool.Dispose();
        Log.Information("Command pool disposed (single buffer auto-freed)");
    }

    [PerFrame]
    [StateFunction("print_on_frame")]
    public static void PrintOnFrame(IDummyValueManager dummyValueManager)
    {
        Log.Information("Dummy Value fetched for {Id} as: {Value}", DummyID.MinimalSampleMod.Hello1, dummyValueManager.GetDummyValue(DummyID.MinimalSampleMod.Hello1));
        Thread.Sleep(1000);
    }
    
    
}