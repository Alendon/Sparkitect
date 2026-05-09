using System.Linq;
using System.Reflection;
using Sparkitect.RenderGraph;

namespace Sparkitect.Tests.RenderGraph;

public class ComputePassTests
{
    private sealed class OrderedTrackingPass : ComputePass
    {
        public List<string> Calls { get; } = new();

        public override void Setup() => Calls.Add("user-setup");
        public override void Execute(in ComputePassExecutePayload payload) => Calls.Add("user-execute");

        protected override void InvokeSlotSetupHooks()
        {
            Calls.Add("slot-setup");
            base.InvokeSlotSetupHooks();
        }

        protected override void InvokeSlotExecuteHooks(in ComputePassExecutePayload payload)
        {
            Calls.Add("slot-execute");
            base.InvokeSlotExecuteHooks(in payload);
        }
    }

    private sealed class DefaultsOnlyPass : ComputePass
    {
        public List<string> Calls { get; } = new();
        public override void Setup() => Calls.Add("user-setup");
        public override void Execute(in ComputePassExecutePayload payload) => Calls.Add("user-execute");
        // intentionally NOT overriding InvokeSlotSetupHooks / InvokeSlotExecuteHooks
    }

    [Test]
    public async Task Setup_ViaHookInterface_InvokesSlotHelperBeforeUserMethod()
    {
        var pass = new OrderedTrackingPass();
        ((ISetupHook)pass).Setup();
        await Assert.That(pass.Calls).IsEquivalentTo(new[] { "slot-setup", "user-setup" });
    }

    [Test]
    public async Task Execute_ViaHookInterface_InvokesSlotHelperBeforeUserMethod()
    {
        var pass = new OrderedTrackingPass();
        var payload = default(ComputePassExecutePayload);
        ((IExecuteHook)pass).Execute(in payload);
        await Assert.That(pass.Calls).IsEquivalentTo(new[] { "slot-execute", "user-execute" });
    }

    [Test]
    public async Task DefaultSlotHelpers_AreNoOps_OnlyUserMethodRecorded()
    {
        var pass = new DefaultsOnlyPass();
        ((ISetupHook)pass).Setup();
        var payload = default(ComputePassExecutePayload);
        ((IExecuteHook)pass).Execute(in payload);
        await Assert.That(pass.Calls).IsEquivalentTo(new[] { "user-setup", "user-execute" });
    }

    [Test]
    public async Task ComputePassExecutePayload_IsReadonlyStruct()
    {
        var t = typeof(ComputePassExecutePayload);
        await Assert.That(t.IsValueType).IsTrue();
        // IsReadOnlyAttribute is the compiler-emitted marker for `readonly struct`.
        var hasReadOnlyMarker = t.GetCustomAttributes(true)
            .Any(a => a.GetType().FullName == "System.Runtime.CompilerServices.IsReadOnlyAttribute");
        await Assert.That(hasReadOnlyMarker).IsTrue();
    }

    [Test]
    public async Task ComputePassExecutePayload_ExposesCommandBufferOnly()
    {
        var props = typeof(ComputePassExecutePayload)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance);
        await Assert.That(props.Length).IsEqualTo(1);
        await Assert.That(props[0].Name).IsEqualTo("CommandBuffer");
        await Assert.That(props[0].PropertyType.Name).IsEqualTo("VkCommandBuffer");
    }

    [Test]
    public async Task ComputePass_ImplementsBothHookInterfaces()
    {
        var t = typeof(ComputePass);
        await Assert.That(typeof(ISetupHook).IsAssignableFrom(t)).IsTrue();
        await Assert.That(typeof(IExecuteHook).IsAssignableFrom(t)).IsTrue();
        await Assert.That(typeof(IPass).IsAssignableFrom(t)).IsTrue();
    }
}
