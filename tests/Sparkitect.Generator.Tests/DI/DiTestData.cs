namespace Sparkitect.Generator.Tests.DI;

public static class DiTestData
{
    public static (string, object) GlobalUsings => ("GlobalUsings.cs",
        """
        global using System;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        global using System.Net.Http;
        global using System.Threading;
        global using System.Threading.Task;
        """);

    public static (string, object) DiAttributes => ("DiAttributes.cs",
        """
        // ReSharper disable once CheckNamespace
        namespace Sparkitect.DI.GeneratorAttributes;
        
        public enum FactoryGenerationType
        {
            Service,
            Factory,
            Entrypoint
        }
        
        [AttributeUsage(AttributeTargets.Class)]
        public class FactoryGenerationTypeAttribute(FactoryGenerationType generationType) : Attribute;
        
        /// <summary>
        /// Marker interface for factory attributes that generate service factories
        /// </summary>
        public interface IFactoryMarker<TExposedType> where TExposedType : class;
        
        [FactoryGenerationType(FactoryGenerationType.Entrypoint)]
        public class EntrypointFactoryAttribute<TBase> : Attribute, IFactoryMarker<TBase> where TBase : class;
        
        /// <summary>
        /// Marks a property parameter (/named argument) as the key for a KeyedFactory.
        /// The parameter must be of type string
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class KeyAttribute : Attribute;
        
        /// <summary>
        /// Marks a property parameter (/named argument) as containing the name of a static property that provides the key for a KeyedFactory.
        /// The static property must return string, Identification, or OneOf&lt;Identification, string&gt;
        /// </summary>
        [AttributeUsage(AttributeTargets.Property)]
        public class KeyPropertyAttribute : Attribute;
        
        [FactoryGenerationType(FactoryGenerationType.Factory)]
        public class KeyedFactoryAttribute<TBase> : Attribute, IFactoryMarker<TBase> where TBase : class
        {
            [Key]
            public string? Key { get; set; }
            
            [KeyProperty]
            public string? KeyPropertyName { get; set; }
        }
        
        [FactoryGenerationType(FactoryGenerationType.Service)]
        public class CreateServiceFactoryAttribute<TInterface> : Attribute, IFactoryMarker<TInterface> where TInterface : class;
        
        [FactoryGenerationType(FactoryGenerationType.Service)]
        public class SingletonAttribute<TInterface> : Attribute, IFactoryMarker<TInterface> where TInterface : class;
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