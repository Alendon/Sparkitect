namespace Sparkitect.Generator.Tests;

public static partial class TestData
{
    public static (string, object) GlobalUsings => ("GlobalUsings.cs",
        """
        global using System;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        global using System.Net.Http;
        global using System.Threading;
        global using System.Threading.Tasks;
        """);

    public static (string, object) MetadataTypes => ("MetadataTypes.cs",
        """
        namespace Sparkitect.Metadata
        {
            [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
            public sealed class MetadataCategoryMarkerAttribute : Attribute;

            [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
            public abstract class MetadataAttribute<TMetadata> : Attribute;
        }
        """);

    public static (string, object) Sparkitect => ("Core.cs",
            """
            using System.Runtime.InteropServices;

            namespace Sparkitect.Modding
            {
                [StructLayout(LayoutKind.Explicit, Size = sizeof(ulong))]
                public readonly struct Identification : IEquatable<Identification>
                {
                    [FieldOffset(0)] public readonly ushort ModId;
                    [FieldOffset(sizeof(ushort))] public readonly ushort CategoryId;
                    [FieldOffset(sizeof(ushort) * 2)] public readonly uint ItemId;

                    public static readonly Identification Empty = Create(0, 0, 0);

                    private Identification(ushort modId, ushort categoryId, uint itemId)
                    {
                        ModId = modId;
                        CategoryId = categoryId;
                        ItemId = itemId;
                    }

                    public static Identification Create(ushort modId, ushort categoryId, uint itemId)
                    {
                        return new Identification(modId, categoryId, itemId);
                    }

                    public bool Equals(Identification other)
                    {
                        return ModId == other.ModId && CategoryId == other.CategoryId && ItemId == other.ItemId;
                    }

                    public override bool Equals(object? obj)
                    {
                        return obj is Identification other && Equals(other);
                    }

                    public override int GetHashCode()
                    {
                        return HashCode.Combine(ModId, CategoryId, ItemId);
                    }
                }

                public interface IHasIdentification
                {
                    public static abstract Identification Identification { get; }
                }

                public interface IRegistryBase
                {
                    void Unregister(Identification id);
                }

                public interface IRegistry : IRegistryBase
                {
                }
            }
            """
        );
}