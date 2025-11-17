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
public sealed class OnStateEnterAttribute : Attribute;

[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnStateExitAttribute : Attribute;

[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnModuleEnterAttribute : Attribute;

[PublicAPI]
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnModuleExitAttribute : Attribute;

[PublicAPI]
[AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
public sealed class StateFacadeAttribute<TFacade> : FacadeMarkerAttribute<TFacade> where TFacade : class;

[PublicAPI]
[FactoryGenerationType(FactoryGenerationType.Service)]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class StateServiceAttribute<TInterface> : Attribute, IFactoryMarker<TInterface> where TInterface : class;

