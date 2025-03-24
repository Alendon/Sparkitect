using System;
using JetBrains.Annotations;

namespace Sparkitect.DI;

/// <summary>
/// Marks a class as a registrations entrypoint
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[MeansImplicitUse]
public class RegistrationsEntrypointAttribute : Attribute
{
}