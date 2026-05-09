using System.Linq;
using System.Reflection;
using Moq;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;
using Sparkitect.RenderGraph;

namespace Sparkitect.Tests.RenderGraph;

public class RenderGraphModuleTests
{
    [Test]
    public async Task RequiredModules_ContainsVulkan()
    {
        var required = RenderGraphModule.RequiredModules;
        await Assert.That(required).Contains(StateModuleID.Sparkitect.Vulkan);
    }

    [Test]
    public async Task AddRenderPassRegistry_CallsAddRegistryOnceForRenderPassRegistry()
    {
        var mock = new Mock<IRegistryManager>(MockBehavior.Strict);
        mock.Setup(m => m.AddRegistry<RenderPassRegistry>()).Verifiable();

        RenderGraphModule.AddRenderPassRegistry(mock.Object);

        mock.Verify(m => m.AddRegistry<RenderPassRegistry>(), Times.Once);
    }

    [Test]
    public async Task ProcessRenderPassRegistry_CallsProcessAllMissingOnceForRenderPassRegistry()
    {
        var mock = new Mock<IRegistryManager>(MockBehavior.Strict);
        mock.Setup(m => m.ProcessAllMissing<RenderPassRegistry>()).Verifiable();

        RenderGraphModule.ProcessRenderPassRegistry(mock.Object);

        mock.Verify(m => m.ProcessAllMissing<RenderPassRegistry>(), Times.Once);
    }

    [Test]
    public async Task RemoveRenderPassRegistry_CallsUnregisterAllRemainingOnceForRenderPassRegistry()
    {
        var mock = new Mock<IRegistryManager>(MockBehavior.Strict);
        mock.Setup(m => m.UnregisterAllRemaining<RenderPassRegistry>()).Verifiable();

        RenderGraphModule.RemoveRenderPassRegistry(mock.Object);

        mock.Verify(m => m.UnregisterAllRemaining<RenderPassRegistry>(), Times.Once);
    }

    [Test]
    public async Task TransitionFunctions_CarryExpectedAttributes()
    {
        var addMethod = typeof(RenderGraphModule)
            .GetMethod("AddRenderPassRegistry", BindingFlags.Public | BindingFlags.Static)!;
        var addAttrNames = addMethod.GetCustomAttributes(false)
            .Select(a => a.GetType().Name).ToList();
        await Assert.That(addAttrNames).Contains("TransitionFunctionAttribute");
        await Assert.That(addAttrNames).Contains("OnCreateSchedulingAttribute");

        var processMethod = typeof(RenderGraphModule)
            .GetMethod("ProcessRenderPassRegistry", BindingFlags.Public | BindingFlags.Static)!;
        var procAttrNames = processMethod.GetCustomAttributes(false)
            .Select(a => a.GetType().Name).ToList();
        await Assert.That(procAttrNames).Contains("TransitionFunctionAttribute");
        await Assert.That(procAttrNames).Contains("OnFrameEnterSchedulingAttribute");
        // OrderAfter<AddRenderPassRegistryFunc> — generic-attribute name carries arity suffix.
        await Assert.That(procAttrNames.Any(n => n.StartsWith("OrderAfterAttribute"))).IsTrue();

        var removeMethod = typeof(RenderGraphModule)
            .GetMethod("RemoveRenderPassRegistry", BindingFlags.Public | BindingFlags.Static)!;
        var rmAttrNames = removeMethod.GetCustomAttributes(false)
            .Select(a => a.GetType().Name).ToList();
        await Assert.That(rmAttrNames).Contains("TransitionFunctionAttribute");
        await Assert.That(rmAttrNames).Contains("OnDestroySchedulingAttribute");
    }
}
