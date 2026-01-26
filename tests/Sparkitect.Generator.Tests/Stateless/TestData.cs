namespace Sparkitect.Generator.Tests;

public static partial class TestData
{
    /// <summary>
    /// Core Stateless ecosystem types - exact name replication from Sparkitect.Stateless namespace.
    /// These types are required for the source generator to function correctly.
    /// </summary>
    public static (string, object) StatelessCoreTypes => ("StatelessCore.cs",
        """
        using System.Diagnostics;
        using Sparkitect.Modding;

        namespace Sparkitect.Stateless
        {
            /// <summary>
            /// Internal interface for source-generated stateless function wrappers.
            /// </summary>
            public interface IStatelessFunction
            {
                [DebuggerStepThrough]
                public void Execute();

                public void Initialize(object container, IReadOnlyDictionary<Type, Type> facadeMap);

                public Identification Identification { get; }
                public Identification ParentIdentication { get; }
            }

            // Non-generic base class for passing generic type without sub-types
            public abstract class StatelessFunctionAttribute : Attribute
            {
                public abstract string Identifier { get; }
            }

            /// <summary>
            /// Base attribute for all stateless functions.
            /// </summary>
            [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
            public abstract class StatelessFunctionAttribute<TContext, TRegistry> : StatelessFunctionAttribute
                where TContext : class
                where TRegistry : IRegistry
            {
                public override string Identifier { get; }

                protected StatelessFunctionAttribute(string identifier) => Identifier = identifier;
            }

            /// <summary>
            /// Base class for scheduling attributes.
            /// </summary>
            [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
            public abstract class SchedulingAttribute<TScheduling, TStatelessFunction, TContext, TRegistry> : Attribute
                where TScheduling : IScheduling<TStatelessFunction, TContext, TRegistry>
                where TStatelessFunction : StatelessFunctionAttribute<TContext, TRegistry>
                where TContext : class
                where TRegistry : IRegistry;

            /// <summary>
            /// Marker base for scheduling parameter attributes.
            /// </summary>
            [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
            public abstract class SchedulingParameterAttribute : Attribute;

            [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
            public abstract class OrderBeforeAttribute() : Attribute
            {
                public abstract Identification Other { get; }
                public abstract bool Optional { get; }
            }

            [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
            public abstract class OrderAfterAttribute() : Attribute
            {
                public abstract Identification Other { get; }
                public abstract bool Optional { get; }
            }

            [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
            public sealed class OrderBeforeAttribute<TOtherFunction>() : OrderBeforeAttribute
                where TOtherFunction : IStatelessFunction, IHasIdentification
            {
                public override Identification Other => TOtherFunction.Identification;
                public override bool Optional => IsOptional;
                public bool IsOptional { get; set; } = false;
            }

            [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
            public sealed class OrderAfterAttribute<TOtherFunction>() : OrderAfterAttribute
                where TOtherFunction : IStatelessFunction, IHasIdentification
            {
                public override Identification Other => TOtherFunction.Identification;
                public override bool Optional => IsOptional;
                public bool IsOptional { get; set; } = false;
            }

            /// <summary>
            /// Defines a scheduling implementation for stateless functions.
            /// </summary>
            public interface IScheduling<TStatelessFunction, TContext, TRegistry>
                where TStatelessFunction : StatelessFunctionAttribute<TContext, TRegistry>
                where TContext : class
                where TRegistry : IRegistry
            {
                void BuildGraph(IExecutionGraphBuilder builder, TContext context, Identification functionId, Identification ownerId);
            }

            public interface IExecutionGraphBuilder
            {
                void AddNode(Identification node);
                void AddEdge(Identification from, Identification to, bool optional);
                IReadOnlyList<Identification> Resolve();
            }
        }
        """);

    /// <summary>
    /// Test-specific implementations for Stateless Function tests.
    /// Uses test-friendly names (TestRegistry, TestContext, etc.).
    /// </summary>
    public static (string, object) StatelessTestTypes => ("StatelessTestTypes.cs",
        """
        using Sparkitect.Modding;
        using Sparkitect.Stateless;
        using Sparkitect.DI.GeneratorAttributes;

        namespace StatelessTest
        {
            /// <summary>
            /// Test registry for stateless function tests.
            /// </summary>
            [Registry(Identifier = "test")]
            public partial class TestRegistry : IRegistry
            {
                public static string Identifier => "test";

                public void Unregister(Identification id) { }
            }

            /// <summary>
            /// Simple test context for stateless function scheduling.
            /// </summary>
            public class TestContext
            {
                public bool IsActive { get; set; } = true;
            }

            /// <summary>
            /// Test function attribute extending StatelessFunctionAttribute.
            /// </summary>
            [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
            public sealed class TestFunctionAttribute(string identifier)
                : StatelessFunctionAttribute<TestContext, TestRegistry>(identifier);

            /// <summary>
            /// Test scheduling attribute.
            /// </summary>
            [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
            public sealed class TestSchedulingAttribute
                : SchedulingAttribute<TestScheduling, TestFunctionAttribute, TestContext, TestRegistry>;

            /// <summary>
            /// Test scheduling implementation.
            /// </summary>
            public sealed class TestScheduling : IScheduling<TestFunctionAttribute, TestContext, TestRegistry>
            {
                private readonly OrderAfterAttribute[]? _orderAfter;
                private readonly OrderBeforeAttribute[]? _orderBefore;

                public TestScheduling(OrderAfterAttribute[]? orderAfter, OrderBeforeAttribute[]? orderBefore)
                {
                    _orderAfter = orderAfter;
                    _orderBefore = orderBefore;
                }

                public void BuildGraph(IExecutionGraphBuilder builder, TestContext context, Identification functionId, Identification ownerId)
                {
                    if (!context.IsActive) return;

                    builder.AddNode(functionId);

                    if (_orderAfter != null)
                    {
                        foreach (var after in _orderAfter)
                        {
                            builder.AddEdge(after.Other, functionId, after.Optional);
                        }
                    }

                    if (_orderBefore != null)
                    {
                        foreach (var before in _orderBefore)
                        {
                            builder.AddEdge(functionId, before.Other, before.Optional);
                        }
                    }
                }
            }
        }
        """);
}
