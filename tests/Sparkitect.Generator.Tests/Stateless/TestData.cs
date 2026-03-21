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
            public abstract class SchedulingAttribute<TScheduling> : Sparkitect.Metadata.MetadataAttribute<TScheduling>
                where TScheduling : IScheduling;

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
            /// Explicitly specifies the parent (owner) of a stateless function.
            /// Takes priority over containing class's IHasIdentification.
            /// </summary>
            [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
            public abstract class ParentIdAttribute : Attribute;

            /// <summary>
            /// Explicitly specifies the parent (owner) of a stateless function.
            /// Takes priority over containing class's IHasIdentification.
            /// </summary>
            [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
            public sealed class ParentIdAttribute<TOwner> : ParentIdAttribute
                where TOwner : IHasIdentification;

            /// <summary>
            /// Base interface for scheduling metadata types.
            /// </summary>
            public interface IScheduling
            {
                Identification OwnerId { get; set; }
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
                : SchedulingAttribute<TestScheduling>;

            /// <summary>
            /// Test scheduling implementation.
            /// </summary>
            public sealed class TestScheduling : IScheduling
            {
                private readonly OrderAfterAttribute[]? _orderAfter;
                private readonly OrderBeforeAttribute[]? _orderBefore;

                public Identification OwnerId { get; set; }

                public TestScheduling(OrderAfterAttribute[]? orderAfter, OrderBeforeAttribute[]? orderBefore)
                {
                    _orderAfter = orderAfter;
                    _orderBefore = orderBefore;
                }

                public void BuildGraph(IExecutionGraphBuilder builder, TestContext context, Identification functionId)
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
