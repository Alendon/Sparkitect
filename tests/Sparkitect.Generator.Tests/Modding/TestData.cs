// ReSharper disable once CheckNamespace
namespace Sparkitect.Generator.Tests;

public static partial class TestData
{
    public static (string, object) ModdingCode => ("Modding.cs",
        """
        using Sparkitect.DI.GeneratorAttributes;
        
        namespace Sparkitect.Modding;
        
        [FactoryGenerationType(FactoryGenerationType.Factory)]
        public class RegistryAttribute : Attribute, IFactoryMarker<IRegistry>
        {
            [Key] public required string Identifier { get; set; }
        }
        
        public class RegistryMetadataAttribute<TMetadata> : Attribute where TMetadata : class;
        
        public interface IRegistry;
        
        
        """);
}