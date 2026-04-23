using Moq;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.Vma;

namespace Sparkitect.Tests.Graphics.Vulkan.Vma;

public class VmaServiceTests
{
    [Test]
    public async Task DefaultAllocator_BeforeInitialize_ThrowsInvalidOperationException()
    {
        var mockVulkanContext = new Mock<IVulkanContext>();
        var service = new VmaService { VulkanContext = mockVulkanContext.Object };

        await Assert.That(() => { _ = service.DefaultAllocator; }).Throws<InvalidOperationException>();
    }
}
