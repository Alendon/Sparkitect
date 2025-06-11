using Sparkitect.DI.GeneratorAttributes;

namespace Sparkitect.Modding;

[FactoryGenerationType(FactoryGenerationType.Factory)]
public class RegistryAttribute : Attribute, IFactoryMarker<IRegistry>
{
    [Key] public required string Identifier { get; set; }
}

[AttributeUsage(AttributeTargets.Assembly)]
public class RegistryMetadataAttribute<TMetadata> : Attribute where TMetadata : class;

[AttributeUsage(AttributeTargets.Method)]
public class RegistryMethodAttribute : Attribute;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class UseResourceFileAttribute : Attribute
{
    public required string Identifier { get; set; }
    public bool Required { get; set; } = false;
}
