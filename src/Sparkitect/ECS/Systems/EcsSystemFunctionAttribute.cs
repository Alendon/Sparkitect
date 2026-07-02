using JetBrains.Annotations;
using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.ECS.Systems;

/// <summary>
/// Marks a static method as an ECS system function, registered under the given identifier in the
/// system registry and executed with an <see cref="EcsSystemContext"/>.
/// </summary>
[EcsSystemCategory]
[RegistrationMarker("ecs_system")]
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
[MeansImplicitUse]
[PublicAPI]
public sealed class EcsSystemFunctionAttribute(string identifier)
    : StatelessFunctionAttribute<EcsSystemContext, SystemRegistry>(identifier);

/// <summary>
/// Attaches ordering scheduling metadata (<see cref="EcsSystemScheduling"/>) to an ECS system function.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
[MeansImplicitUse]
[PublicAPI]
public sealed class EcsSystemSchedulingAttribute : SchedulingAttribute<EcsSystemScheduling>;
