using Sparkitect.Graphics.Vulkan.VulkanObjects;

namespace Sparkitect.RenderGraph;

/// <summary>
/// Execute-time payload handed to compute-category passes. Carries only the command
/// buffer the pass body may record into; resource access goes through pass-constructor
/// DI per D-E3.
/// </summary>
/// <remarks>
/// <para>
/// Per Phase 49 D-A2 / D-B4 / D-E2 this is a <c>readonly struct</c>. Choice of
/// <c>readonly struct</c> over <c>ref struct</c> keeps fields addable in Phase 51+
/// (compute-dispatch helper context) without an API break. No bind / dispatch /
/// clear helpers ship at walking-skeleton — Phase 51 introduces those when the first
/// compute-dispatch-based pass type ships.
/// </para>
/// </remarks>
public readonly struct ComputePassExecutePayload
{
    public VkCommandBuffer CommandBuffer { get; }

    public ComputePassExecutePayload(VkCommandBuffer commandBuffer)
    {
        CommandBuffer = commandBuffer;
    }
}
