using System;

namespace Sparkitect.DI;

/// <summary>
/// Marks a class as a registrations entrypoint
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class RegistrationsEntrypointAttribute : Attribute
{
}