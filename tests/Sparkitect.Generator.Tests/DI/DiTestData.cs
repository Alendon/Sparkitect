namespace Sparkitect.Generator.Tests.DI;

public static class DiTestData
{
    public static (string, object) DiAttributes => ("DiAttributes.cs",
        """
        using System;
        namespace Sparkitect.DI.GeneratorAttributes
        {
            [AttributeUsage(AttributeTargets.Class)]
            [FactoryGenerationType(FactoryGenerationType.Service)]
            public class SingletonAttribute<TInterface> : FactoryAttribute<TInterface> where TInterface : class;
            
            [AttributeUsage(AttributeTargets.Class)]
            public class ServiceFactoryAttribute<TService> : Attribute where TService : class;
            
            [AttributeUsage(AttributeTargets.Class)]
            public class EntrypointFactoryAttribute<TBase> : Attribute where TBase : class;
            
            [AttributeUsage(AttributeTargets.Class)]
            public class KeyedFactoryAttribute<TBase> : Attribute where TBase : class
            {
                public KeyedFactoryAttribute([Key] string? key = null, [KeyProperty] string? propertyName = null)
                {
                }
            }
            
            [AttributeUsage(AttributeTargets.Parameter)]
            public class KeyAttribute : Attribute;
            
            [AttributeUsage(AttributeTargets.Parameter)]
            public class KeyPropertyAttribute : Attribute;
            
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
            public class EntrypointAttribute<TBase> : FactoryAttribute<TBase> where TBase : class;
            
            [AttributeUsage(AttributeTargets.Class)]
            [FactoryGenerationType(FactoryGenerationType.Factory)]
            public class KeyedAttribute<TBase> : FactoryAttribute<TBase> where TBase : class;
            
            [AttributeUsage(AttributeTargets.Class)]
            [FactoryGenerationType(FactoryGenerationType.Factory)]
            public class KeyedFactory<TBase> : FactoryAttribute<TBase> where TBase : class;
        }
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