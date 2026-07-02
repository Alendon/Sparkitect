using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.ECS.Systems;

/// <summary>
/// A node in the system group tree. Groups have IsGroup=true and may contain children.
/// Systems are leaves with IsGroup=false and empty children list.
/// State is mutable -- toggling state does not trigger graph rebuild.
/// </summary>
[PublicAPI]
public class SystemTreeNode
{
    /// <summary>The identification of this system or group.</summary>
    public Identification Id { get; }

    /// <summary>Mutable execution state; toggling it does not trigger a graph rebuild.</summary>
    public SystemState State { get; set; }

    /// <summary>Child nodes; empty for system leaves.</summary>
    public List<SystemTreeNode> Children { get; }

    /// <summary>True when this node is a group, false when it is a system leaf.</summary>
    public bool IsGroup { get; }

    /// <summary>Creates a node for the given id; groups pass <paramref name="isGroup"/> true.</summary>
    public SystemTreeNode(Identification id, bool isGroup, SystemState state = SystemState.Active)
    {
        Id = id;
        IsGroup = isGroup;
        State = state;
        Children = new List<SystemTreeNode>();
    }
}
