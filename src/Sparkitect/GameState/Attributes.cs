using JetBrains.Annotations;
using Sparkitect.DI.GeneratorAttributes;

namespace Sparkitect.GameState;

[PublicAPI]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class OrderModuleBeforeAttribute<TModule>() : Attribute where TModule : IStateModule
{
}

[PublicAPI]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class OrderModuleAfterAttribute<TModule>() : Attribute where TModule : IStateModule
{
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

[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class OrderBeforeAttribute<TModuleOrState>(string key) : Attribute
{
    public string Key { get; } = key;
}

[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public sealed class OrderAfterAttribute<TModuleOrState>(string key) : Attribute
{
    public string Key { get; } = key;
}




[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class StateFunctionAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}


[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class PerFrameAttribute : Attribute;

[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnCreateAttribute : Attribute;

[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnDestroyAttribute : Attribute;

[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnFrameEnterAttribute : Attribute;

[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnFrameExitAttribute : Attribute;

[PublicAPI]
[AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
public sealed class StateFacadeAttribute<TFacade> : FacadeMarkerAttribute<TFacade> where TFacade : class;

/// <summary>
/// Non-generic marker attribute for StateFacade entrypoint discovery
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class StateFacadeAttribute : Attribute;

[PublicAPI]
[FactoryGenerationType(FactoryGenerationType.Service)]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class StateServiceAttribute<TInterface, TModule> : Attribute, IFactoryMarker<TInterface>
    where TInterface : class
    where TModule : IStateModule;

