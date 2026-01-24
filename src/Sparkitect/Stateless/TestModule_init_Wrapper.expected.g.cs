//HintName: TestModule_init_Wrapper.g.cs
// Expected SG output for TestModule.Initialize - Transition function (OnCreate)

using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

#pragma warning disable CS9113
#pragma warning disable CS1591

namespace Sparkitect.Graphics.Vulkan;

public partial class VulkanModule
{
    public class VulkanInitFunc : global::Sparkitect.Stateless.IStatelessFunction, IHasIdentification
    {
        public Identification Identification => TransitionFunctionID.Sparkitect.VulkanInit;
        //IHasIdentification is required but can't live in IStatelessFunction chain
        static Identification IHasIdentification.Identification => TransitionFunctionID.Sparkitect.VulkanInit;
        
        //VulkanModule => any Type implementing IHasIdentification
        public Identification ParentIdentication => VulkanModule.Identification;
        

        private IVulkanContextStateFacade _param0;

        [global::System.Diagnostics.DebuggerStepThroughAttribute]
        public void Execute()
        {
            VulkanModule.VulkanInit(_param0);
        }

        public void Initialize(
            global::Sparkitect.DI.Container.ICoreContainer container,
            global::System.Collections.Generic.IReadOnlyDictionary<global::System.Type, global::System.Type> facadeMap)
        {
            if (!container.TryResolveMapped<IVulkanContextStateFacade>(out _param0, facadeMap))
            {
                throw new global::System.InvalidOperationException(
                    "Failed to resolve global::GameStateTest.ITestService for stateless function init");
            }
        }
    }
}