using JetBrains.Annotations;

namespace Sparkitect.Modding;

/// <summary>
/// Chooses the inspection granularity of mod AssemblyLoadContexts: one per state-stack mod group, or
/// one per mod. Both modes resolve assemblies identically — the setting only changes unload attribution.
/// </summary>
[PublicAPI]
public enum AlcGranularity
{
    /// <summary>One ALC per state-stack mod group, as today. The static default.</summary>
    PerGroup = 0,

    /// <summary>
    /// One ALC per mod, chained by dependency topo order. Preferred for debug environments: exact
    /// per-mod unload attribution.
    /// </summary>
    PerMod = 1
}
