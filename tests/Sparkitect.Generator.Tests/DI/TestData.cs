namespace Sparkitect.Generator.Tests;

public static partial class TestData
{
    public static (string, object) DiAttributes => ("DiAttributes.cs",
        """
        // ReSharper disable once CheckNamespace
        namespace Sparkitect.DI.GeneratorAttributes;

        /// <summary>
        /// Base marker attribute for facade types
        /// </summary>
        public abstract class FacadeMarkerAttribute<TFacade> : Attribute where TFacade : class;
        """);

    public static (string, object) DiPipelineAttributes => ("DiPipelineAttributes.cs",
        """
        namespace Sparkitect.Modding;

        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
        public sealed class OptionalModDependentAttribute : Attribute
        {
            public string ModId { get; }
            public OptionalModDependentAttribute(string modId) => ModId = modId;
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        public sealed class ModLoadedGuardAttribute : Attribute
        {
            public string ModId { get; }
            public ModLoadedGuardAttribute(string modId) => ModId = modId;
        }
        """);

    public static (string, object) DiContainerInterfaces => ("DiContainerInterfaces.cs",
        """
        namespace Sparkitect.DI.Container;

        public interface ICoreContainerBuilder
        {
            void Register<TFactory>();
            bool TryResolveInternal<T>(out T result) where T : class;
            bool TryResolveInternal(Type type, out object result);
        }

        public interface IFactoryContainerBuilder<TBase>
        {
            void Register(object factory);
        }
        """);
}
