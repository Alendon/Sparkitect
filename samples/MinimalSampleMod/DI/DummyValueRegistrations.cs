using MinimalSampleMod.DI;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.Modding;
using Sparkitect.Modding.IDs;


namespace MinimalSampleMod.DI
{
    // The registrations classes are source generated
    // they contain the object registration logic and also store the Identification values
    [RegistrationsEntrypoint]
    public class DummyValueRegistrations(IIdentificationManager identificationManager) : Registrations<DummyRegistry>
    {
        public override string CategoryIdentifier => "dummy";

        // Store the value of the 'Hello' Id in a static field for quick access
        public static Identification Hello { get; private set; }

        public override void PrePhaseRegistration(DummyRegistry registry, ICoreContainer container)
        {
        }

        public override void MainPhaseRegistration(DummyRegistry dummyRegistry, ICoreContainer container)
        {
            // Of course a real implementation checks if the resolve is successfull
            // This is just a simple example
            container.TryResolve(out object diDependency);

            // Fetch the value to register
            string value = RegistryExample.SomeValueToRegister(diDependency);

            // Register an id for the object
            Hello = identificationManager.RegisterObject("minimal_sample_mod", "dummy", "hello");

            // Invoke the actual register method of the registry with the id and value
            dummyRegistry.Register(Hello, value);
        }

        public override void PostPhaseRegistration(DummyRegistry registry, ICoreContainer container)
        {
        }
    }
}

namespace Sparkitect.Modding.IDs
{
    // One empty stub class for holding the ID Values, get generated per RegistryCategory
    public static class DummyID
    {

    }


    public static class DummyIDExtension
    {
        // For each Mod one or multiple extensions can be created per ID Value Container
        extension(DummyID)
        {

            // Currently the IDs are organized into groups by the mod.
            // This can later be organized in any way. The extension feature should provide all necessary capabilities for this
            public static SampleModDummyIDs SampleMod => default;
        }
    }

    // The sub containers are just (readonly) structs, with properties for each Identification
    public readonly struct SampleModDummyIDs
    {
        // The actual values are stored in the Registrations classes.
        // The string ids are expected to be in snake_case (compiler/analyzer error if not)
        // The names of the generated properties are the PascalCase variant ("my_new_dummy" => "MyNewDummy")
        public Identification Hello => DummyValueRegistrations.Hello;
    }

}