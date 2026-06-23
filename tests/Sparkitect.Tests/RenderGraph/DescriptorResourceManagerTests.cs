using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Moq;
using Silk.NET.Vulkan;
using Sparkitect.Graphics.RenderGraph_Deprecated;
using Sparkitect.Graphics.RenderGraph_Deprecated.Resources;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Graphics.Vulkan.VulkanObjects;
using Sparkitect.Modding;
using Sparkitect.Utils;
using Sparkitect.Utils.DU;
using VkApiResult = Silk.NET.Vulkan.Result;

namespace Sparkitect.Tests.RenderGraph;

public class DescriptorResourceManagerTests
{
    private static readonly Identification PassA = Identification.Create(1, 1, 1);

    private static VkDescriptorSetLayout FakeLayout() =>
        (VkDescriptorSetLayout)RuntimeHelpers.GetUninitializedObject(typeof(VkDescriptorSetLayout));

    /// <summary>
    /// Mock context capturing the options handed to <see cref="IVulkanContext.CreateDescriptorSetLayout"/>
    /// and returning an Ok layout.
    /// </summary>
    private static (DescriptorResourceManager Manager, Func<VkDescriptorSetLayoutCreateOptions?> CapturedOptions)
        ManagerCapturing()
    {
        VkDescriptorSetLayoutCreateOptions? captured = null;
        var ctx = new Mock<IVulkanContext>(MockBehavior.Loose);
        ctx.Setup(c => c.CreateDescriptorSetLayout(
                It.IsAny<VkDescriptorSetLayoutCreateOptions>(), It.IsAny<CallerContext>()))
            .Returns((VkDescriptorSetLayoutCreateOptions o, CallerContext _) =>
            {
                captured = o;
                return new Result<VkDescriptorSetLayout, VkApiResult>.Ok(FakeLayout());
            });
        return (new DescriptorResourceManager(ctx.Object), () => captured);
    }

    private static DescriptorBinding Binding(uint binding, uint arrayIndex, DescriptorType type) =>
        new(binding, arrayIndex, new FakeViewHandle(new FakeBindingSource(type)));

    [Test]
    public async Task Declare_StorageImageBinding_DerivesPushFlaggedLayout()
    {
        var (mgr, capturedOptions) = ManagerCapturing();
        IDescriptorResourceManager typed = mgr;

        var request = new DescriptorRequest(
            ImmutableArray.Create(Binding(0, 0, DescriptorType.StorageImage)),
            ShaderStageFlags.ComputeBit);

        var handle = typed.Declare(PassA, 0, request);

        var opts = capturedOptions();
        await Assert.That(opts).IsNotNull();
        await Assert.That(opts!.Flags).IsEqualTo(DescriptorSetLayoutCreateFlags.PushDescriptorBitKhr);
        await Assert.That(opts.Bindings.Length).IsEqualTo(1);

        var b = opts.Bindings[0];
        await Assert.That(b.DescriptorType).IsEqualTo(DescriptorType.StorageImage);
        await Assert.That(b.DescriptorCount).IsEqualTo(1u);
        await Assert.That(b.StageFlags).IsEqualTo(ShaderStageFlags.ComputeBit);
        await Assert.That(handle.Fetch().SetLayout).IsNotNull();
    }

    [Test]
    public async Task Declare_DuplicateBindingSlot_Throws()
    {
        var (mgr, _) = ManagerCapturing();
        IDescriptorResourceManager typed = mgr;
        var request = new DescriptorRequest(ImmutableArray.Create(
            Binding(0, 0, DescriptorType.StorageImage),
            Binding(0, 0, DescriptorType.StorageBuffer)));

        await Assert.That(() => typed.Declare(PassA, 3, request))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Declare_EmptyBindingSet_Throws()
    {
        var (mgr, _) = ManagerCapturing();
        IDescriptorResourceManager typed = mgr;
        var request = new DescriptorRequest(ImmutableArray<DescriptorBinding>.Empty);

        await Assert.That(() => typed.Declare(PassA, 7, request))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Declare_UnresolvedBoundView_Throws()
    {
        var (mgr, _) = ManagerCapturing();
        IDescriptorResourceManager typed = mgr;
        var request = new DescriptorRequest(ImmutableArray.Create(
            new DescriptorBinding(0, 0, new FakeViewHandle(source: null))));

        await Assert.That(() => typed.Declare(PassA, 2, request))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Declare_UnsupportedDescriptorType_Throws()
    {
        var (mgr, _) = ManagerCapturing();
        IDescriptorResourceManager typed = mgr;
        var request = new DescriptorRequest(ImmutableArray.Create(
            Binding(0, 0, DescriptorType.SampledImage)));

        await Assert.That(() => typed.Declare(PassA, 4, request))
            .Throws<InvalidOperationException>();
    }

    private sealed class FakeBindingSource : IDescriptorBindingSource
    {
        public FakeBindingSource(DescriptorType type) => DescriptorType = type;
        public DescriptorType DescriptorType { get; }
        public DescriptorBindingPayload DescribeBinding() =>
            throw new InvalidOperationException("DescribeBinding is Execute-time only and not used by Declare.");
    }

    private sealed class FakeViewHandle : IGraphResource<IDescriptorBindingSource>
    {
        private readonly IDescriptorBindingSource? _source;
        public FakeViewHandle(IDescriptorBindingSource? source) => _source = source;
        public int Slot => 0;
        public IDescriptorBindingSource Fetch() =>
            _source ?? throw new InvalidOperationException("view not bound");
    }
}
