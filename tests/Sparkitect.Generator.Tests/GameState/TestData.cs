namespace Sparkitect.Generator.Tests;

public static partial class TestData
{
    public static (string, object) GameStateAttributes => ("GameStateAttributes.cs",
        """
        using Sparkitect.DI.GeneratorAttributes;

        namespace Sparkitect.GameState;

        // Core interfaces
        public interface IStateModule
        {
            static abstract Sparkitect.Modding.Identification Identification { get; }
            static abstract IReadOnlyList<Type> UsedServices { get; }
        }

        public interface IStateDescriptor
        {
            static abstract Sparkitect.Modding.Identification ParentId { get; }
            static abstract Sparkitect.Modding.Identification Identification { get; }
            static abstract IReadOnlyList<Sparkitect.Modding.Identification> Modules { get; }
        }

        public interface IStateMethod
        {
            void Execute();
            void Initialize(Sparkitect.DI.Container.ICoreContainer container, System.Collections.Generic.IReadOnlyDictionary<System.Type, System.Type> facadeMap);
        }

        // Schedule enum
        public enum StateMethodSchedule
        {
            PerFrame,
            OnStateEnter,
            OnStateExit,
            OnModuleEnter,
            OnModuleExit
        }

        // State function attribute
        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
        public sealed class StateFunctionAttribute(string key) : Attribute
        {
            public string Key { get; } = key;
        }

        // Scheduling attributes
        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
        public sealed class PerFrameAttribute : Attribute;

        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
        public sealed class OnStateEnterAttribute : Attribute;

        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
        public sealed class OnStateExitAttribute : Attribute;

        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
        public sealed class OnModuleEnterAttribute : Attribute;

        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
        public sealed class OnModuleExitAttribute : Attribute;

        // Ordering attributes
        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
        public sealed class OrderBeforeAttribute(string key) : Attribute
        {
            public string Key { get; } = key;
        }

        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
        public sealed class OrderAfterAttribute(string key) : Attribute
        {
            public string Key { get; } = key;
        }

        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
        public sealed class OrderBeforeAttribute<TModuleOrState>(string key) : Attribute
        {
            public string Key { get; } = key;
        }

        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
        public sealed class OrderAfterAttribute<TModuleOrState>(string key) : Attribute
        {
            public string Key { get; } = key;
        }

        // Module ordering attributes
        [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
        public sealed class OrderModuleBeforeAttribute<TModule>() : Attribute where TModule : IStateModule;

        [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
        public sealed class OrderModuleAfterAttribute<TModule>() : Attribute where TModule : IStateModule;

        // Association and ordering infrastructure
        [AttributeUsage(AttributeTargets.Class)]
        public sealed class StateMethodAssociationEntrypointAttribute : Attribute;

        public sealed class StateMethodAssociationBuilder
        {
            public void Add(Sparkitect.Modding.Identification parentId, string methodKey, Type wrapperType, StateMethodSchedule schedule) { }
        }

        public abstract class StateMethodAssociation
        {
            public abstract void Configure(StateMethodAssociationBuilder builder);
        }

        public sealed class IStateMethodOrderingEntrypointAttribute : Attribute;

        public record OrderingEntry((Sparkitect.Modding.Identification Parent, string Method) Before, (Sparkitect.Modding.Identification Parent, string Method) After);

        public abstract class StateMethodOrdering
        {
            public abstract void ConfigureOrdering(HashSet<OrderingEntry> ordering);
        }

        namespace Sparkitect.DI.Container
        {
            public interface ICoreContainer
            {
                bool TryResolve<T>(out T result) where T : class;
                bool TryResolveMapped<T>(out T result, System.Collections.Generic.IReadOnlyDictionary<System.Type, System.Type> facadeMap) where T : class;
            }
        }

        [AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
        public sealed class StateFacadeAttribute<TFacade> : FacadeMarkerAttribute<TFacade> where TFacade : class;

        [FactoryGenerationType(FactoryGenerationType.Service)]
        [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
        public sealed class StateServiceAttribute<TInterface> : Attribute, IFactoryMarker<TInterface> where TInterface : class;
        """);
}