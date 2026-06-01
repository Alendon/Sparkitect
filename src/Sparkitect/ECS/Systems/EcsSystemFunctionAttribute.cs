using JetBrains.Annotations;
using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.ECS.Systems;

[EcsSystemCategory]
[RegistrationMarker("ecs_system")]
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
[MeansImplicitUse]
[PublicAPI]
public sealed class EcsSystemFunctionAttribute(string identifier)
    : StatelessFunctionAttribute<EcsSystemContext, SystemRegistry>(identifier);

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
[MeansImplicitUse]
[PublicAPI]
public sealed class EcsSystemSchedulingAttribute : SchedulingAttribute<EcsSystemScheduling>;
