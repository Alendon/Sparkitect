using System.Runtime.CompilerServices;
using Moq;
using Sparkitect.DI.Container;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.RenderGraph;
using Sparkitect.Windowing;

namespace Sparkitect.Tests.RenderGraph;

public class RenderGraphResolutionProviderTests
{
    private interface IUnknownToProvider
    {
        // marker — must NOT match the provider's intercepted serviceType set
    }

    private static (RenderGraphResolutionProvider provider,
                    Mock<ICoreContainer> hostMock,
                    Mock<ISparkitWindow> windowMock,
                    VkSwapchain swapchainSentinel,
                    Mock<IRenderGraphFrameContext> frameCtxMock) Build()
    {
        var host = new Mock<ICoreContainer>(MockBehavior.Strict);
        var window = new Mock<ISparkitWindow>(MockBehavior.Loose);
        // VkSwapchain has only a Vulkan-driven public constructor (requires real device);
        // Moq cannot mock a class with no parameterless ctor without invoking the real ctor,
        // and the real ctor calls Vk.TryGetDeviceExtension which needs a live Vulkan instance.
        // RuntimeHelpers.GetUninitializedObject produces an instance bypassing all ctors —
        // safe here because the provider only uses the reference identity of _swapchain
        // (it never reads any property or calls any method).
        var swapchain = (VkSwapchain)RuntimeHelpers.GetUninitializedObject(typeof(VkSwapchain));
        var frameCtx = new Mock<IRenderGraphFrameContext>(MockBehavior.Loose);
        var provider = new RenderGraphResolutionProvider(window.Object, swapchain, frameCtx.Object);
        return (provider, host, window, swapchain, frameCtx);
    }

    [Test]
    public async Task TryResolve_RequestsSparkitWindow_ReturnsInjectedInstance()
    {
        var (provider, host, window, _, _) = Build();

        var resolved = provider.TryResolve(typeof(ISparkitWindow), host.Object, [], out var service);

        await Assert.That(resolved).IsTrue();
        await Assert.That(service).IsSameReferenceAs(window.Object);
    }

    [Test]
    public async Task TryResolve_RequestsVkSwapchain_ReturnsInjectedInstance()
    {
        var (provider, host, _, swapchain, _) = Build();

        var resolved = provider.TryResolve(typeof(VkSwapchain), host.Object, [], out var service);

        await Assert.That(resolved).IsTrue();
        await Assert.That(service).IsSameReferenceAs(swapchain);
    }

    [Test]
    public async Task TryResolve_RequestsIRenderGraphFrameContext_ReturnsInjectedInstance()
    {
        var (provider, host, _, _, frameCtx) = Build();

        var resolved = provider.TryResolve(typeof(IRenderGraphFrameContext), host.Object, [], out var service);

        await Assert.That(resolved).IsTrue();
        await Assert.That(service).IsSameReferenceAs(frameCtx.Object);
    }

    [Test]
    public async Task TryResolve_RequestsUnknownType_ReturnsFalseAndNullService()
    {
        // This is THE seam — returning false here lets ResolutionScope.cs:46-52 fall through
        // to _container.TryResolve(serviceType, out service) on the host container.
        var (provider, host, _, _, _) = Build();

        var resolved = provider.TryResolve(typeof(IUnknownToProvider), host.Object, [], out var service);

        await Assert.That(resolved).IsFalse();
        await Assert.That(service).IsNull();
    }
}
