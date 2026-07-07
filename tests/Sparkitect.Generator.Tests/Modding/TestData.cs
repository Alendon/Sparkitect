// ReSharper disable once CheckNamespace
namespace Sparkitect.Generator.Tests;

public static partial class TestData
{
    public static (string, object) ModdingCode => ("Modding.cs",
        """
        using Sparkitect.DI.GeneratorAttributes;

        namespace Sparkitect.Modding;

        public interface IRegisterMarker { }

        public class RegistryAttribute : Attribute
        {
            public required string Identifier { get; set; }
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

        public struct Identification { }
        public readonly struct Identification<T> { }
        public interface IHasIdentification { }

        [AttributeUsage(AttributeTargets.GenericParameter)]
        public sealed class TypedIdentificationAttribute : Attribute { }

        public interface IRegistryBase { void Unregister(Identification id); }
        public interface IRegistry : IRegistryBase { }

        public interface IStateModule { }

        public interface IRegistry<TModule> : IRegistry { }

        // Owning-module stand-in for registry fixtures. Exposes a static Identification the generator's
        // OwningModule emission reads; deliberately does not declare : IHasIdentification so the shared
        // surface never trips the IHasIdentification analyzer in unrelated tests.
        public sealed partial class TestModule
        {
            public static Identification Identification => default;
        }

        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
        public sealed class KeyedFactoryGenerationMarkerAttribute<TBase> : Attribute where TBase : class { }
        """);
}