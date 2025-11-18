using Sparkitect.DI.GeneratorAttributes;

namespace Sparkitect.Modding;

[FactoryGenerationType(FactoryGenerationType.Factory)]
public class RegistryAttribute : Attribute, IFactoryMarker<IRegistryBase>
{
    [Key] public required string Identifier { get; set; }
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class RegistryMetadataAttribute<TMetadata> : Attribute where TMetadata : class;

[AttributeUsage(AttributeTargets.Method)]
public class RegistryMethodAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class UseResourceFileAttribute : Attribute
{
    public required string Identifier { get; set; }
    public bool Required { get; set; } = false;
}

[AttributeUsage(AttributeTargets.Interface)]
public class RegistryFacadeAttribute<TFacade> : FacadeMarkerAttribute<TFacade> where TFacade : class;