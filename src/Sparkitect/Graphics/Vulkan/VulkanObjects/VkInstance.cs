using Silk.NET.Core;
using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Sparkitect.Utils;

namespace Sparkitect.Graphics.Vulkan.VulkanObjects;

public class VkInstance : VulkanObject
{
    private readonly unsafe AllocationCallbacks* _allocationCallbacks;

    private unsafe VkInstance(IObjectTracker<VulkanObject> objectTracker, Vk vk, Instance handle, AllocationCallbacks* allocationCallbacks) : base(
        objectTracker, vk)
    {
        Handle = handle;
        _allocationCallbacks = allocationCallbacks;
    }

    public Instance Handle { get; private set; }

    public static unsafe VkInstance Create(IVulkanContext vulkanContext,
        ICliArgumentHandler cliArgumentHandler)
    {
        var vk = vulkanContext.VkApi;
        
        

        var applicationName = SilkMarshal.StringToPtr("Sparkitect");
        var engineName = SilkMarshal.StringToPtr("Sparkitect");

        var appInfo = new ApplicationInfo()
        {
            SType = StructureType.ApplicationInfo,
            PApplicationName = (byte*)applicationName,
            ApplicationVersion = new Version32(0, 1, 0),
            PEngineName = (byte*)engineName,
            EngineVersion = new Version32(0, 1, 0),
            ApiVersion = Vk.Version13
        };


        List<string> additionalLayers = [];
        List<string> additionalExtensions = [];
        
        additionalLayers.AddRange(cliArgumentHandler.GetArgumentValues("addVkLayer"));
        additionalExtensions.AddRange(cliArgumentHandler.GetArgumentValues("addVkExtension"));
        
        //TODO Trigger BeforeCreateInstance event here
        //To support modification through mods

        IntPtr layerPtr = SilkMarshal.StringArrayToPtr(additionalLayers);
        IntPtr extensionPtr = SilkMarshal.StringArrayToPtr(additionalExtensions);

        var instanceCreateInfo = new InstanceCreateInfo()
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &appInfo,
            EnabledLayerCount = (uint)additionalLayers.Count,
            PpEnabledLayerNames = (byte**)layerPtr,
            EnabledExtensionCount = (uint)additionalExtensions.Count,
            PpEnabledExtensionNames = (byte**)extensionPtr,
        };
        
        var result = vk.CreateInstance(instanceCreateInfo, vulkanContext.DefaultAllocationCallbacks, out var instance);
        
        SilkMarshal.Free(layerPtr);
        SilkMarshal.Free(extensionPtr);
        
        
        throw new NotImplementedException("Vulkan instance creation is not implemented yet.");
    }

    private static void SanitizeExtensionNames(List<string> extensionNames, Vk vk)
    {
        var extensionNamesCopy = extensionNames[..];
        
        foreach (var extensionName in extensionNamesCopy)
        {
            if (vk.IsInstanceExtensionPresent(extensionName)) continue;
            
            Console.WriteLine($"Extension {extensionName} is not supported by the Vulkan instance.");
            extensionNames.Remove(extensionName);
        }
    }
    
    private static void SanitizeLayerNames(List<string> layerNames, Vk vk)
    {
        var layerNamesCopy = layerNames[..];
        
        
        foreach (var layerName in layerNamesCopy)
        {
            Console.WriteLine($"Layer {layerName} is not supported by the Vulkan instance.");
            layerNames.Remove(layerName);
        }
    }


    public override unsafe void Destroy()
    {
        Vk.DestroyInstance(Handle, _allocationCallbacks);
    }
}