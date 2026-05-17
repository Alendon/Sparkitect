using JetBrains.Annotations;
using Sparkitect.Stateless;

namespace Sparkitect.ECS.Systems;

[EcsSystemCategory]
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
[MeansImplicitUse]
public sealed class EcsSystemFunctionAttribute(string identifier)
    : StatelessFunctionAttribute<EcsSystemContext, SystemRegistry>(identifier);

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
[MeansImplicitUse]
public sealed class EcsSystemSchedulingAttribute : SchedulingAttribute<EcsSystemScheduling>;
