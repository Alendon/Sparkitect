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
    // This is a method/property registry
    [RegistryMethod]
    public void RegisterValue(Identification id, string value)
    {
        dummyValueManager.AddDummyValue(id, value);
    }
    
    // It is allowed, to define a single generic argument for method/property registries
    [RegistryMethod]
    public void RegisterGenericValue<SomeType>(Identification id, SomeType value)
    {
    }
    
    //If only a generic argument is used and no further parameter, it is a Type Registry
    [RegistryMethod]
    public void RegisterType<SomeType>(Identification id)
    {
    }
    
    //If no direct argument is provided, the registry happens through the resource file
    //This is intended to be used with files associated with the id. EG registering textures
    [RegistryMethod]
    public void RegisterResourceFile(Identification id)
    {
    }
    
}

public static class RegistryExample
{
    // For the Register method is automatically a RegisterValueAttribute generated in the DummyRegistry
    // With this we can mark a (int this case) method to serve as a registry value provider
    // Aka we instruct the source generator to add a registry object for the DummyRegistry,
    // with the object string id "hello" and use the SomeValueToRegister method to provide the register value
    // The parameters of this function are trying to be fetched from the DI core container
    // If they are not annotated nullable, the startup fails if these types cannot be found in the container.
    [DummyRegistry.RegisterValue("hello1")]
    public static string SomeValueToRegister(object dummyDiObject) => "Hello World " + dummyDiObject;

    //The RegisterGenericValueAttribute is currently not generated. @Codex fix this issue
    [DummyRegistry.RegisterGenericValue("hello2")]
    public static string SomeGenericValueToRegister(string dummyDiObject) => "Hello World " + dummyDiObject;

    //The RegisterTypeAttribute is currently not generated. @Codex fix this issue
    [DummyRegistry.RegisterType("hello3")]
    public class SampleType
    {
        
    }
    
    /*
     * @Codex The current resource yaml parsing is more a stub than really usable
     * Your task is to also fix on this.
     * A yaml file can either has a "registries" root entry, which contains a list of registry entry
     * Or it starts directly with a singular registry root entry
     * The yaml content should be minimal, and only contain what is actually needed
     * The main usage is to define new id's and associate files with it
     * Depending on the Registry configuration, either a single file or multiple (determined by key)
     * 
     */
    
    
    public static void UsageSample()
    {
        // Utilize the extension to have a statically typed access to the Hello Identification
        // While maintaining a coherent/easy access
        Identification helloId = DummyID.SampleMod.Hello1;
    }
}