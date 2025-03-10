namespace Sparkitect.Modding;

/// <summary>
/// Defines possible phases of the registry process
/// Can be used by/for the registry and identification manager
/// </summary>
[Flags]
public enum RegistryPhase
{
    None = 0,
    Mod = 1,
    Category = 2,
    ObjectPre = 4,
    ObjectMain = 8,
    ObjectPost = 16,
}