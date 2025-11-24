namespace Sparkitect.Graphics.Vulkan.Vma;

[Flags]
public enum VmaAllocationCreateFlags
{
    None = 0,
    DedicatedMemory = 1 << 0,
    NeverAllocate = 1 << 1,
    Mapped = 1 << 2,
    UserDataCopyString = 1 << 5,
    UpperAddress = 1 << 6,
    DontBind = 1 << 7,
    WithinBudget = 1 << 8,
    CanAlias = 1 << 9,
    HostAccessSequentialWrite = 1 << 10,
    HostAccessRandom = 1 << 11,
    HostAccessAllowTransferInstead = 1 << 12,
    StrategyMinMemory = 1 << 16,
    StrategyMinTime = 1 << 17,
    StrategyMinOffset = 1 << 18,
}
