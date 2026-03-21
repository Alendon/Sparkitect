namespace Sparkitect.Generator.Tests.Metadata;

public static class MetadataTestData
{
    /// <summary>
    /// ApplyMetadataEntrypoint runtime types needed for MetadataGenerator tests.
    /// </summary>
    public static (string, object) ApplyMetadataEntrypointTypes => ("ApplyMetadataEntrypoint.cs",
        """
        using Sparkitect.Modding;

        namespace Sparkitect.Metadata
        {
            public class ApplyMetadataEntrypointAttribute<TMetadata> : Attribute;

            public abstract class ApplyMetadataEntrypoint<TMetadata>
            {
                public abstract void CollectMetadata(Dictionary<Identification, TMetadata> metadata);
            }
        }

        namespace Sparkitect.Modding
        {
            public static class IdentificationHelper
            {
                public static Identification Read<T>() where T : IHasIdentification => T.Identification;
            }
        }
        """);

    /// <summary>
    /// Test metadata types -- a simple metadata class and attribute for MetadataGenerator tests.
    /// </summary>
    public static (string, object) TestMetadataTypes => ("TestMetadataTypes.cs",
        """
        using Sparkitect.Metadata;

        namespace MetadataTest
        {
            /// <summary>
            /// Simple test metadata type with no-arg constructor.
            /// </summary>
            public class TestMetadataType
            {
                public TestMetadataType() { }
            }

            /// <summary>
            /// Test metadata attribute inheriting MetadataAttribute with [MetadataCategoryMarker].
            /// </summary>
            [MetadataCategoryMarker]
            [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
            public sealed class TestMetadataAttribute : MetadataAttribute<TestMetadataType>;
        }
        """);
}
