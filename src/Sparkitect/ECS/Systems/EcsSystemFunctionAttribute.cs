using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.ECS.Systems;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class EcsSystemFunctionAttribute(string identifier)
    : StatelessFunctionAttribute<EcsSystemContext, SystemRegistry>(identifier);

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class EcsSystemSchedulingAttribute
    : SchedulingAttribute<EcsSystemScheduling, EcsSystemFunctionAttribute, EcsSystemContext, SystemRegistry, IEcsGraphBuilder>;
