using Sparkitect.Modding;

namespace Sparkitect.ECS.Queries;

/// <summary>
/// Declares components for read-only access in a component query.
/// Multiple attributes can be stacked on the same class to exceed the 9-arity limit.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ReadComponents<T1> : ComponentAccessAttribute
    where T1 : unmanaged, IHasIdentification;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ReadComponents<T1, T2> : ComponentAccessAttribute
    where T1 : unmanaged, IHasIdentification
    where T2 : unmanaged, IHasIdentification;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ReadComponents<T1, T2, T3> : ComponentAccessAttribute
    where T1 : unmanaged, IHasIdentification
    where T2 : unmanaged, IHasIdentification
    where T3 : unmanaged, IHasIdentification;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ReadComponents<T1, T2, T3, T4> : ComponentAccessAttribute
    where T1 : unmanaged, IHasIdentification
    where T2 : unmanaged, IHasIdentification
    where T3 : unmanaged, IHasIdentification
    where T4 : unmanaged, IHasIdentification;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ReadComponents<T1, T2, T3, T4, T5> : ComponentAccessAttribute
    where T1 : unmanaged, IHasIdentification
    where T2 : unmanaged, IHasIdentification
    where T3 : unmanaged, IHasIdentification
    where T4 : unmanaged, IHasIdentification
    where T5 : unmanaged, IHasIdentification;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ReadComponents<T1, T2, T3, T4, T5, T6> : ComponentAccessAttribute
    where T1 : unmanaged, IHasIdentification
    where T2 : unmanaged, IHasIdentification
    where T3 : unmanaged, IHasIdentification
    where T4 : unmanaged, IHasIdentification
    where T5 : unmanaged, IHasIdentification
    where T6 : unmanaged, IHasIdentification;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ReadComponents<T1, T2, T3, T4, T5, T6, T7> : ComponentAccessAttribute
    where T1 : unmanaged, IHasIdentification
    where T2 : unmanaged, IHasIdentification
    where T3 : unmanaged, IHasIdentification
    where T4 : unmanaged, IHasIdentification
    where T5 : unmanaged, IHasIdentification
    where T6 : unmanaged, IHasIdentification
    where T7 : unmanaged, IHasIdentification;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ReadComponents<T1, T2, T3, T4, T5, T6, T7, T8> : ComponentAccessAttribute
    where T1 : unmanaged, IHasIdentification
    where T2 : unmanaged, IHasIdentification
    where T3 : unmanaged, IHasIdentification
    where T4 : unmanaged, IHasIdentification
    where T5 : unmanaged, IHasIdentification
    where T6 : unmanaged, IHasIdentification
    where T7 : unmanaged, IHasIdentification
    where T8 : unmanaged, IHasIdentification;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class ReadComponents<T1, T2, T3, T4, T5, T6, T7, T8, T9> : ComponentAccessAttribute
    where T1 : unmanaged, IHasIdentification
    where T2 : unmanaged, IHasIdentification
    where T3 : unmanaged, IHasIdentification
    where T4 : unmanaged, IHasIdentification
    where T5 : unmanaged, IHasIdentification
    where T6 : unmanaged, IHasIdentification
    where T7 : unmanaged, IHasIdentification
    where T8 : unmanaged, IHasIdentification
    where T9 : unmanaged, IHasIdentification;
