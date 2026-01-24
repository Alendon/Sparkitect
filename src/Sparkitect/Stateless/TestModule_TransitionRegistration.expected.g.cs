//HintName: TestModule_TransitionRegistration.g.cs
// Expected SG output for TestModule transition function registration
// NOTE: Actual registration structure handled by Registry Generator helper.
// The stateless function SG ensures registry is called - implementation details
// are outside its concern.

using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Graphics.Vulkan;
using Sparkitect.Stateless;

#pragma warning disable CS9113
#pragma warning disable CS1591

namespace GameStateTest;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.DI.RegistrationsEntrypoint<TransitionRegistry>]
internal class TestModule_TransitionRegistration
    : global::Sparkitect.DI.Registrations<TransitionRegistry>
{
    public override string CategoryIdentifier => "transition_function";

    public static global::Sparkitect.Modding.Identification VulkanInit { get; private set; }
    
    public override void ProcessRegistrations(TransitionRegistry registry)
    {
        {
            VulkanInit = IdentificationManager.RegisterObject("sparkitect", "transition_function", "vulkan_init");
            registry.Register<VulkanModule.VulkanInitFunc>(VulkanInit);
        }
    }
}