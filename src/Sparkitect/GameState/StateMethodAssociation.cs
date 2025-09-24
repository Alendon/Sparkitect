using Sparkitect.DI;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

[AttributeUsage(AttributeTargets.Class)]
public sealed class StateMethodAssociationEntrypointAttribute : Attribute;

public enum StateMethodSchedule
{
    PerFrame,
    OnStateEnter,
    OnStateExit,
    OnModuleEnter,
    OnModuleExit
}

public sealed class StateMethodAssociationBuilder
{
    private readonly Dictionary<(Identification ParentId, string MethodKey, StateMethodSchedule Schedule), Type> _registrations = new();

    public void Add(Identification parentId, string methodKey, Type wrapperType, StateMethodSchedule schedule)
    {
        _registrations[(parentId, methodKey, schedule)] = wrapperType;
    }

    public bool Remove(Identification parentId, string methodKey, StateMethodSchedule schedule)
    {
        return _registrations.Remove((parentId, methodKey, schedule));
    }

    public void Clear()
    {
        _registrations.Clear();
    }

    public IReadOnlyDictionary<(Identification ParentId, string MethodKey, StateMethodSchedule Schedule), Type> Build()
    {
        return _registrations;
    }
}

public abstract class StateMethodAssociation : IConfigurationEntrypoint<StateMethodAssociationEntrypointAttribute>
{
    public abstract void Configure(StateMethodAssociationBuilder builder);
}
