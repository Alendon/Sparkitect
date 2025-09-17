using JetBrains.Annotations;

namespace Sparkitect.GameState;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class StateModuleAttribute(string id) : Attribute
{
    public string Id { get; } = id;
}

[PublicAPI]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class OrderBeforeModuleAttribute(Type moduleType) : Attribute
{
    public Type ModuleType { get; } = moduleType;
}

[PublicAPI]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class OrderAfterModuleAttribute(Type moduleType) : Attribute
{
    public Type ModuleType { get; } = moduleType;
}

[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class OrderBeforeAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}

[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class OrderAfterAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}

public enum TransitionTrigger
{
    Removed,
    UnchangedBefore,
    UnchangedAfter,
    Add
}

[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class TransitionAttribute(TransitionTrigger trigger, string key) : Attribute
{
    public TransitionTrigger Trigger { get; } = trigger;
    public string Key { get; } = key;
}

[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class FeatureAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}

[PublicAPI]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class StateServiceAttribute<TModule, TExposed> : Attribute
    where TModule : class
    where TExposed : class
{
    public Type ModuleType => typeof(TModule);
    public Type ExposedType => typeof(TExposed);

    public Type? Facade { get; init; }
}

