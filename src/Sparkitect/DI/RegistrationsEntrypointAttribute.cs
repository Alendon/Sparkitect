using JetBrains.Annotations;
using Sparkitect.Modding;

namespace Sparkitect.DI;

/// <summary>
/// Marks a class as a registrations entrypoint
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[MeansImplicitUse]
public class RegistrationsEntrypointAttribute<TRegistry> : Attribute where TRegistry : class, IRegistry
{
}