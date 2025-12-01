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
        
        [AttributeUsage(AttributeTargets.Assembly)]
        public class RegistryMetadataAttribute<TMetadata> : Attribute where TMetadata : class;
        
        [AttributeUsage(AttributeTargets.Method)]
        public class RegistryMethodAttribute : Attribute;
        
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        public class UseResourceFileAttribute : Attribute
        {
            public required string Key { get; set; }
            public bool Required { get; set; } = false;
            public bool Primary { get; set; } = false;
        }

        [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = true)]
        public sealed class RegistryFacadeAttribute<TFacade> : Sparkitect.DI.GeneratorAttributes.FacadeMarkerAttribute<TFacade> where TFacade : class;
        """);
}