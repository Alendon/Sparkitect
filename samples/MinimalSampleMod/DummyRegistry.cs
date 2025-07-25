using Sparkitect.Modding;
using Sparkitect.Modding.IDs;

namespace MinimalSampleMod;


// define the string identifier for this registry (/category)
[Registry(Identifier = "dummy")]
// partial class to allow source gen extensions.
// The registry is Di instantiated
public partial class DummyRegistry(IDummyValueManager dummyValueManager) : IRegistry
{
    // Define a registry method. Check the SG implementation to determine the possible registry method types
    [RegistryMethod]
    public void Register(Identification id, string value)
    {
        dummyValueManager.AddDummyValue(id, value);
    }
    
}

public static class RegistryExample
{
    // For the Register method is automatically a RegisterAttribute generated in the DummyRegistry
    // With this we can mark a (int this case) method to serve as a registry value provider
    // Aka we instruct the source generator to add a registry object for the DummyRegistry,
    // with the object string id "hello" and use the SomeValueToRegister method to provide the register value
    // The parameters of this function are trying to be fetched from the DI core container
    // If they are not annotated nullable, the startup fails if these types cannot be found in the container.
    [DummyRegistry.Register("hello")]
    public static string SomeValueToRegister(object dummyDiObject) => "Hello World " + dummyDiObject;

    public static void UsageSample()
    {
        // Utilize the extension to have a statically typed access to the Hello Identification
        // While maintaining a coherent/easy access
        Identification helloId = DummyID.SampleMod.Hello;
    }
}