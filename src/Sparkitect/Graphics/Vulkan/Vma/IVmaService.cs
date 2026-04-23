using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.Vma;

/// <summary>
/// Engine service exposing one VMA allocator per active Vulkan device plus an
/// on-demand factory for additional managed allocators.
/// </summary>
/// <remarks>
/// <para>
/// Registered via <c>[StateService&lt;IVmaService, VulkanModule&gt;]</c> on
/// <see cref="VmaService"/>. The service's lifecycle is tied to the
/// <see cref="Sparkitect.Graphics.Vulkan.VulkanModule"/> — resolvable from any
/// state whose active modules include Vulkan.
/// </para>
/// <para>
/// Consumers typically resolve via required-property injection:
/// <code>
/// public required IVmaService VmaService { private get; init; }
/// // ...
/// var buffer = VmaService.DefaultAllocator.CreateBuffer(bufferInfo, allocInfo);
/// </code>
/// </para>
/// </remarks>
public interface IVmaService
{
    /// <summary>
    /// The shared default managed allocator. Non-null after <see cref="Initialize"/>
    /// has been invoked by the <c>create_vma</c> transition; accessing before that
    /// throws <see cref="InvalidOperationException"/>.
    /// </summary>
    ManagedVmaAllocator DefaultAllocator { get; }

    /// <summary>
    /// Creates a new managed allocator on demand, separate from
    /// <see cref="DefaultAllocator"/>. The returned allocator is owned by the caller
    /// and must be <see cref="IDisposable.Dispose"/>d explicitly; the service tracks
    /// it and will emit a leak log if it is still alive at service disposal.
    /// </summary>
    /// <param name="callerContext">Compile-time-injected call-site for leak attribution.</param>
    ManagedVmaAllocator CreateAllocator([InjectCallerContext] CallerContext callerContext = default);

    /// <summary>
    /// Eagerly constructs <see cref="DefaultAllocator"/>. <strong>Called by the
    /// <c>create_vma</c> transition function on <see cref="Sparkitect.Graphics.Vulkan.VulkanModule"/>;
    /// not intended for general consumer use.</strong>
    /// </summary>
    void Initialize();

    /// <summary>
    /// Disposes the default allocator and logs any leaked on-demand allocators.
    /// <strong>Called by the <c>destroy_vma</c> transition function on
    /// <see cref="Sparkitect.Graphics.Vulkan.VulkanModule"/>; not intended for general consumer use.</strong>
    /// </summary>
    void Dispose();
}
