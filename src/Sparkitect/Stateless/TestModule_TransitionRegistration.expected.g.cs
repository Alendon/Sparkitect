//HintName: TestModule_TransitionRegistration.g.cs
// Expected SG output for TestModule transition function registration
// NOTE: Actual registration structure handled by Registry Generator helper.
// The stateless function SG ensures registry is called - implementation details
// are outside its concern.

using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.GameState;
using Sparkitect.Stateless;

#pragma warning disable CS9113
#pragma warning disable CS1591

namespace GameStateTest;

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.Stateless.StatelessRegistrationEntrypoint<global::Sparkitect.Stateless.TransitionFunctionAttribute>]
internal class TestModule_TransitionRegistration
    : global::Sparkitect.Stateless.StatelessRegistration<global::Sparkitect.Stateless.TransitionFunctionAttribute>
{
    public override void Register(TransitionRegistry registry)
    {
        // Registration invocation structure TBD via Registry Generator helper.
        // Functions to register:
        // - TestModule.initWrapper (OnCreate)
        // - TestModule.cleanupWrapper (OnDestroy)
        throw new global::System.NotImplementedException("Stub - actual structure via Registry SG");
        
        // Implementation provided by Registry SG, but not called by Registry SG
        TransitionFunctionIDs.Sparkitect.Init = ...; //register identification
        registry.Register<TestModule.initWrapper>(TransitionFunctionIDs.Sparkitect.Init);
    }
}