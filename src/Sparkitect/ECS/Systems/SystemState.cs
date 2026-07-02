using JetBrains.Annotations;

namespace Sparkitect.ECS.Systems;

/// <summary>Execution state of a system or group node in the tree.</summary>
[PublicAPI]
public enum SystemState
{
    /// <summary>The node executes; groups also execute their subtree.</summary>
    Active,

    /// <summary>The node is skipped; inactive groups skip their entire subtree.</summary>
    Inactive
}
