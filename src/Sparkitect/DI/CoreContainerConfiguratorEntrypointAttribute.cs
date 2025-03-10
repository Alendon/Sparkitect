using System;

namespace Sparkitect.DI;

/// <summary>
/// Marks a class as an IoC builder entrypoint
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class CoreContainerConfiguratorEntrypointAttribute : Attribute
{
}