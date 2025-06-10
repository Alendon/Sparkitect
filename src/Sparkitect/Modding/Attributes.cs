using Sparkitect.DI.GeneratorAttributes;

namespace Sparkitect.Modding;

[FactoryGenerationType(FactoryGenerationType.Factory)]
public class RegistryAttribute : Attribute, IFactoryMarker<IRegistry>
{
    [Key] public required string Identifier { get; set; }
}

[AttributeUsage(AttributeTargets.Assembly)]
public class RegistryMetadataAttribute<TMetadata> : Attribute where TMetadata : class;

