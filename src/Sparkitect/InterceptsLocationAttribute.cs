// ReSharper disable once CheckNamespace
using JetBrains.Annotations;

namespace System.Runtime.CompilerServices;

/// <summary>
/// Marks a method as intercepting calls to another method at a specific source location.
/// Used by source generators for compile-time method interception.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
#pragma warning disable CS9113 // Parameter is unread (read by compiler for interception metadata)
[PublicAPI]
public sealed class InterceptsLocationAttribute(int version, string data) : Attribute
#pragma warning restore CS9113
{
}