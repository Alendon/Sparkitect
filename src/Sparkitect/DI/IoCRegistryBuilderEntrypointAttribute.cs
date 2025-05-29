using JetBrains.Annotations;

namespace Sparkitect.DI;

/// <summary>
/// Marks a class as an IoC registry builder entrypoint
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[MeansImplicitUse]
public class IoCRegistryBuilderEntrypointAttribute : Attribute
{
}