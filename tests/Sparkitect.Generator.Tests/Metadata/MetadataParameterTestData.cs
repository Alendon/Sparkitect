namespace Sparkitect.Generator.Tests.Metadata;

/// <summary>
/// FQN-accurate test-type bundle for MetadataParameterAnalyzer golden tests. Declares the
/// parameter markers, ordering/parent attributes, method-scope scheduling category, class-scope
/// system-group category, and a brand-new class-scope category — all under production namespaces
/// and inheriting the real marker base so the analyzer's structural discovery resolves them.
/// Pairs with TestData.MetadataTypes (MetadataAttribute&lt;T&gt; + MetadataCategoryMarker) and
/// TestData.Sparkitect (Identification + IHasIdentification); those are NOT redeclared here.
/// </summary>
public static class MetadataParameterTestData
{
    /// <summary>The metadata parameter marker base and the placement-analysis opt-out attribute.</summary>
    public static (string, object) Markers => ("MetadataParameterMarkers.cs",
        """
        namespace Sparkitect.Metadata
        {
            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = true, AllowMultiple = true)]
            public abstract class MetadataParameterAttribute : Attribute;

            [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
            public sealed class AllowUnharvestedMetadataParametersAttribute : Attribute;
        }
        """);

    /// <summary>
    /// Ordering + parent markers (inheriting MetadataParameterAttribute), a stateless-function
    /// attribute, and a method-scope scheduling category whose payload harvests Order[]+Order[].
    /// Also a reusable IHasIdentification target for the generic ordering/parent type arguments.
    /// </summary>
    public static (string, object) SchedulingAndOrdering => ("MetadataParameterOrdering.cs",
        """
        using Sparkitect.Metadata;
        using Sparkitect.Modding;

        namespace Sparkitect.Stateless
        {
            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
            public abstract class OrderBeforeAttribute : MetadataParameterAttribute
            {
                public abstract Identification Other { get; }
                public abstract bool Optional { get; }
            }

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
            public abstract class OrderAfterAttribute : MetadataParameterAttribute
            {
                public abstract Identification Other { get; }
                public abstract bool Optional { get; }
            }

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
            public sealed class OrderBeforeAttribute<TOther> : OrderBeforeAttribute
                where TOther : IHasIdentification
            {
                public override Identification Other => TOther.Identification;
                public override bool Optional => IsOptional;
                public bool IsOptional { get; set; }
            }

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
            public sealed class OrderAfterAttribute<TOther> : OrderAfterAttribute
                where TOther : IHasIdentification
            {
                public override Identification Other => TOther.Identification;
                public override bool Optional => IsOptional;
                public bool IsOptional { get; set; }
            }

            public abstract class ParentIdAttribute : MetadataParameterAttribute
            {
                public abstract Identification Other { get; }
            }

            [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
            public sealed class ParentIdAttribute<TOwner> : ParentIdAttribute
                where TOwner : IHasIdentification
            {
                public override Identification Other => TOwner.Identification;
            }

            public abstract class StatelessFunctionAttribute : Attribute;

            [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
            public sealed class StatelessTestFunctionAttribute : StatelessFunctionAttribute;

            [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
            public abstract class SchedulingAttribute<TScheduling> : MetadataAttribute<TScheduling>;

            [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
            public sealed class TestSchedulingAttribute : SchedulingAttribute<TestScheduling>;

            public sealed class TestScheduling
            {
                public TestScheduling(OrderAfterAttribute[] orderAfter, OrderBeforeAttribute[] orderBefore) { }
            }
        }

        namespace MetadataParamTest
        {
            public class NamedTarget : IHasIdentification
            {
                public static Identification Identification => Identification.Empty;
            }
        }
        """);

    /// <summary>
    /// Class-scope system-group category whose payload ctor harvests Order[]+Order[]+ParentId?,
    /// mirroring the production SystemGroupScheduling shape.
    /// </summary>
    public static (string, object) SystemGroupCategory => ("MetadataParameterSystemGroup.cs",
        """
        using Sparkitect.Metadata;
        using Sparkitect.Modding;
        using Sparkitect.Stateless;

        namespace Sparkitect.ECS.Systems
        {
            public class SystemGroupScheduling
            {
                public SystemGroupScheduling(
                    OrderAfterAttribute[] orderAfter,
                    OrderBeforeAttribute[] orderBefore,
                    ParentIdAttribute? parentId) { }
            }

            [MetadataCategoryMarker]
            [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
            public sealed class SystemGroupSchedulingAttribute : MetadataAttribute<SystemGroupScheduling>;
        }
        """);

    /// <summary>
    /// A brand-new class-scope category existing ONLY in test sources — its payload harvests
    /// OrderBefore[]. Proves the analyzer validates a new category with zero analyzer changes.
    /// </summary>
    public static (string, object) NavStepCategory => ("MetadataParameterNavStep.cs",
        """
        using Sparkitect.Metadata;
        using Sparkitect.Stateless;

        namespace NavTest
        {
            public class NavStepConfig
            {
                public NavStepConfig(OrderBeforeAttribute[] orderBefore) { }
            }

            [MetadataCategoryMarker]
            [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
            public sealed class NavStepAttribute : MetadataAttribute<NavStepConfig>;
        }
        """);
}
