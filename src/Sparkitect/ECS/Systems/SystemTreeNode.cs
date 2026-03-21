using Sparkitect.Modding;

namespace Sparkitect.ECS.Systems;

/// <summary>
/// A node in the system group tree. Groups have IsGroup=true and may contain children.
/// Systems are leaves with IsGroup=false and empty children list.
/// State is mutable -- toggling state does not trigger graph rebuild.
/// </summary>
public class SystemTreeNode
{
    public Identification Id { get; }
    public SystemState State { get; set; }
    public List<SystemTreeNode> Children { get; }
    public bool IsGroup { get; }

    public SystemTreeNode(Identification id, bool isGroup, SystemState state = SystemState.Active)
    {
        Id = id;
        IsGroup = isGroup;
        State = state;
        Children = new List<SystemTreeNode>();
    }
}
