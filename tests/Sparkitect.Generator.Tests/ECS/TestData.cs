namespace Sparkitect.Generator.Tests;

public static partial class TestData
{
    public static partial class ECS
    {
        public static (string, object) EcsTypes => ("EcsTypes.cs",
            """
            using System;
            using System.Collections.Generic;
            using Sparkitect.Modding;

            namespace Sparkitect.ECS
            {
                public readonly struct StorageHandle : IEquatable<StorageHandle>
                {
                    public readonly uint Index;
                    public readonly uint Generation;
                    public StorageHandle(uint index, uint generation) { Index = index; Generation = generation; }
                    public bool Equals(StorageHandle other) => Index == other.Index && Generation == other.Generation;
                    public override bool Equals(object? obj) => obj is StorageHandle other && Equals(other);
                    public override int GetHashCode() => HashCode.Combine(Index, Generation);
                }

                public readonly struct FilterHandle : IEquatable<FilterHandle>
                {
                    public readonly int Index;
                    public FilterHandle(int index) { Index = index; }
                    public bool Equals(FilterHandle other) => Index == other.Index;
                    public override bool Equals(object? obj) => obj is FilterHandle other && Equals(other);
                    public override int GetHashCode() => Index.GetHashCode();
                }

                public ref struct StorageAccessor
                {
                    public T? As<T>() where T : class => default;
                }

                public interface IWorld : IDisposable
                {
                    FilterHandle RegisterFilter(
                        IReadOnlyList<Sparkitect.ECS.Capabilities.ICapabilityRequirement> filter,
                        Action<IReadOnlyList<StorageHandle>> callback);
                    void UnregisterFilter(FilterHandle handle);
                    StorageAccessor GetStorage(StorageHandle handle);
                }
            }

            namespace Sparkitect.ECS.Capabilities
            {
                public interface ICapability;
                public interface ICapabilityMetadata;

                public interface ICapabilityRequirement
                {
                    Type CapabilityType { get; }
                }

                public interface ICapabilityRequirement<TCapability, in TMeta> : ICapabilityRequirement
                    where TCapability : ICapability
                    where TMeta : ICapabilityMetadata
                {
                    Type ICapabilityRequirement.CapabilityType => typeof(TCapability);
                    bool Matches(TMeta metadata);
                }

                public interface IChunkedIteration : ICapability
                {
                    bool GetNextChunk(ref Sparkitect.ECS.Storage.ChunkHandle handle, out int length);
                    unsafe byte* GetChunkComponentData(ref Sparkitect.ECS.Storage.ChunkHandle handle, Identification componentId);
                }

                public interface IChunkedIteration<TKey> : IChunkedIteration
                    where TKey : unmanaged
                {
                    TKey GetKey(ref Sparkitect.ECS.Storage.ChunkHandle handle, int index);
                }
            }

            namespace Sparkitect.ECS.Storage
            {
                public struct ChunkHandle
                {
                    internal int Offset;
                    internal bool Complete;
                }
            }

            namespace Sparkitect.ECS.Queries
            {
                public record ComponentSetMetadata(IReadOnlySet<Identification> Components) : Sparkitect.ECS.Capabilities.ICapabilityMetadata;

                public struct ComponentSetRequirement : Sparkitect.ECS.Capabilities.ICapabilityRequirement<Sparkitect.ECS.Capabilities.IChunkedIteration, ComponentSetMetadata>
                {
                    public ComponentSetRequirement(IReadOnlyList<Identification> componentIds) { }
                    public bool Matches(ComponentSetMetadata metadata) => true;
                }

                public struct ComponentSetRequirement<TKey> : Sparkitect.ECS.Capabilities.ICapabilityRequirement<Sparkitect.ECS.Capabilities.IChunkedIteration<TKey>, ComponentSetMetadata>
                    where TKey : unmanaged
                {
                    public ComponentSetRequirement(IReadOnlyList<Identification> componentIds) { }
                    public bool Matches(ComponentSetMetadata metadata) => true;
                }
            }
            """);

        public static (string, object) EcsAttributes => ("EcsAttributes.cs",
            """
            using Sparkitect.Modding;

            namespace Sparkitect.ECS.Queries
            {
                public abstract class ComponentAccessAttribute : Attribute;

                [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                public sealed class ComponentQueryAttribute : Attribute;

                [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
                public sealed class ExposeKeyAttribute<TKey> : Attribute
                    where TKey : unmanaged
                {
                    public bool Required { get; }
                    public ExposeKeyAttribute(bool required) => Required = required;
                }

                [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                public sealed class ReadComponents<T1> : ComponentAccessAttribute
                    where T1 : unmanaged, IHasIdentification;

                [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                public sealed class ReadComponents<T1, T2> : ComponentAccessAttribute
                    where T1 : unmanaged, IHasIdentification
                    where T2 : unmanaged, IHasIdentification;

                [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                public sealed class ReadComponents<T1, T2, T3> : ComponentAccessAttribute
                    where T1 : unmanaged, IHasIdentification
                    where T2 : unmanaged, IHasIdentification
                    where T3 : unmanaged, IHasIdentification;

                [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                public sealed class WriteComponents<T1> : ComponentAccessAttribute
                    where T1 : unmanaged, IHasIdentification;

                [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                public sealed class WriteComponents<T1, T2> : ComponentAccessAttribute
                    where T1 : unmanaged, IHasIdentification
                    where T2 : unmanaged, IHasIdentification;

                [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
                public sealed class ExcludeComponents<T1> : ComponentAccessAttribute
                    where T1 : unmanaged, IHasIdentification;
            }
            """);

        public static (string, object) EcsSystemStubs => ("EcsSystemStubs.cs",
            """
            using Sparkitect.Modding;
            using Sparkitect.Stateless;

            namespace Sparkitect.DI.Resolution
            {
                public interface IResolutionMetadataEntrypoint
                {
                    void ConfigureResolutionMetadata(Dictionary<Type, List<object>> dependencies);
                }
                public interface IResolutionMetadataEntrypoint<TWrapperType> : IResolutionMetadataEntrypoint;

                [AttributeUsage(AttributeTargets.Class, Inherited = false)]
                public sealed class ResolutionMetadataEntrypointAttribute<TWrapperType> : Attribute;
            }

            namespace Sparkitect.ECS.Queries
            {
                public abstract class QueryParameterMetadata
                {
                    public abstract object CreateQuery(Sparkitect.ECS.IWorld world);
                    public abstract void DisposeQuery(object query);
                }

                public class SgQueryMetadata<TQuery> : QueryParameterMetadata where TQuery : IDisposable
                {
                    public SgQueryMetadata(
                        IReadOnlyList<Identification> readComponentIds,
                        IReadOnlyList<Identification> writeComponentIds,
                        Func<Sparkitect.ECS.IWorld, TQuery> factory) { }
                    public override object CreateQuery(Sparkitect.ECS.IWorld world) => null!;
                    public override void DisposeQuery(object query) { }
                }
            }

            namespace Sparkitect.ECS.Systems
            {
                public sealed class EcsSystemContext { }

                [Registry(Identifier = "ecs_system")]
                public sealed partial class SystemRegistry : IRegistry
                {
                    public static string Identifier => "ecs_system";
                    public void Unregister(Identification id) { }
                }

                [AttributeUsage(AttributeTargets.Method, Inherited = false)]
                public sealed class EcsSystemFunctionAttribute(string identifier)
                    : StatelessFunctionAttribute<EcsSystemContext, SystemRegistry>(identifier);

                public sealed class EcsSystemScheduling : IScheduling
                {
                    public Identification OwnerId { get; set; }
                }

                [AttributeUsage(AttributeTargets.Method, Inherited = false)]
                public sealed class EcsSystemSchedulingAttribute
                    : SchedulingAttribute<EcsSystemScheduling>;

                public class FrameTimingHolder { }
            }

            namespace Sparkitect.ECS.Commands
            {
                public interface ICommandBufferAccessor { }
            }
            """);

        public static (string, object) SampleComponents => ("SampleComponents.cs",
            """
            using Sparkitect.Modding;

            namespace TestMod
            {
                public struct Position : IHasIdentification
                {
                    public float X, Y;
                    public static Identification Identification => Identification.Create(1, 1, 1);
                }

                public struct Velocity : IHasIdentification
                {
                    public float Dx, Dy;
                    public static Identification Identification => Identification.Create(1, 1, 2);
                }

                public struct Health : IHasIdentification
                {
                    public int Value;
                    public static Identification Identification => Identification.Create(1, 1, 3);
                }

                public struct EnemyTag : IHasIdentification
                {
                    public static Identification Identification => Identification.Create(1, 1, 4);
                }

                public struct EntityId : IHasIdentification
                {
                    public uint Value;
                    public static Identification Identification => Identification.Create(1, 1, 5);
                }
            }
            """);
    }
}
