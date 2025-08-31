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

        // Store the ID values in  static fields for quick access
        public static Identification Hello1 { get; private set; }
        public static Identification Hello2 { get; private set; }
        public static Identification Hello3 { get; private set; }
        
        //hello4 would be defined in this sample in yaml
        public static Identification Hello4 { get; private set; }

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

            Hello1 = identificationManager.RegisterObject("minimal_sample_mod", "dummy", "hello1");
            dummyRegistry.RegisterValue(Hello1, value);

            var genericValue = RegistryExample.SomeGenericValueToRegister(value);
            Hello2 = identificationManager.RegisterObject("minimal_sample_mod", "dummy", "hello2");
            dummyRegistry.RegisterGenericValue(Hello2, genericValue);
            
            Hello3 = identificationManager.RegisterObject("minimal_sample_mod", "dummy", "hello3");
            dummyRegistry.RegisterType<RegistryExample.SampleType>(Hello3);
            
            Hello4 = identificationManager.RegisterObject("minimal_sample_mod", "dummy", "hello4");
            dummyRegistry.RegisterResourceFile(Hello4);

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
        // Note, this feature is added in C# 14 (preview), it allows direct "extensions" to the specified class
        // Remarks: A extension still needs to be contained in a class
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
        public Identification Hello1 => DummyValueRegistrations.Hello1;
        public Identification Hello2 => DummyValueRegistrations.Hello2;
        public Identification Hello3 => DummyValueRegistrations.Hello3;
        public Identification Hello4 => DummyValueRegistrations.Hello4;
    }

}