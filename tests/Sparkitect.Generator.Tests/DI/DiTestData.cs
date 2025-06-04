namespace Sparkitect.Generator.Tests.DI;

public static class DiTestData
{
    public static (string, object) DiAttributes => ("DiAttributes.cs",
        """
        namespace Sparkitect.DI.GeneratorAttributes;
        
        public enum FactoryGenerationType
        {
            Service,
            Factory,
            Entrypoint
        }
        
        public class FactoryGenerationTypeAttribute(FactoryGenerationType generationType) : Attribute;
        
        [AttributeUsage(AttributeTargets.Class)]
        public abstract class FactoryAttribute<TExposedType> : Attribute where TExposedType : class;
        
        [AttributeUsage(AttributeTargets.Class)]
        [FactoryGenerationType(FactoryGenerationType.Entrypoint)]
        public class EntrypointFactoryAttribute<TBase> : FactoryAttribute<TBase> where TBase : class;
        
        /// <summary>
        /// Marks a constructor parameter as the key for a KeyedFactory.
        /// The parameter must be of type string, Identification, or OneOf&lt;Identification, string&gt;
        /// </summary>
        [AttributeUsage(AttributeTargets.Parameter)]
        public class KeyAttribute : Attribute;
        
        /// <summary>
        /// Marks a constructor parameter as containing the name of a static property that provides the key for a KeyedFactory.
        /// The static property must return string, Identification, or OneOf&lt;Identification, string&gt;
        /// </summary>
        [AttributeUsage(AttributeTargets.Parameter)]
        public class KeyPropertyAttribute : Attribute;
        
        [AttributeUsage(AttributeTargets.Class)]
        [FactoryGenerationType(FactoryGenerationType.Factory)]
        public class KeyedFactoryAttribute<TBase> : FactoryAttribute<TBase> where TBase : class
        {
            public KeyedFactoryAttribute([Key] string? key = null, [KeyProperty] string? propertyName = null)
            {
            }
        }
        
        [AttributeUsage(AttributeTargets.Class)]
        [FactoryGenerationType(FactoryGenerationType.Service)]
        public class CreateServiceFactoryAttribute<TInterface> : FactoryAttribute<TInterface> where TInterface : class;
        
        [AttributeUsage(AttributeTargets.Class)]
        [FactoryGenerationType(FactoryGenerationType.Service)]
        public class SingletonAttribute<TInterface> : FactoryAttribute<TInterface> where TInterface : class;
        """);
    
    public static (string, object) DiStubTypes => ("DiStubTypes.cs", 
        """
        namespace Sparkitect.Modding
        {
            public readonly struct Identification 
            {
                public string Value { get; }
                public Identification(string value) => Value = value;
            }
        }
        
        namespace Sparkitect.DI
        {
            public interface ConfigurationEntrypoint<TDiscoveryAttribute> where TDiscoveryAttribute : Attribute;
        }
        """);
}