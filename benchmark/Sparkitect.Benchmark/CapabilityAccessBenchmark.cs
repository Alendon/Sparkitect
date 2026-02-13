using System;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Sparkitect.Benchmark;

public struct Position { public float X, Y, Z; }

public struct Velocity { public float X, Y, Z; }

public interface IComponentAccessor
{
    void Process(Span<Position> positions, ReadOnlySpan<Velocity> velocities);
}

public struct ArchetypeAccessor : IComponentAccessor
{
    public void Process(Span<Position> positions, ReadOnlySpan<Velocity> velocities)
    {
        for (int i = 0; i < positions.Length; i++)
        {
            positions[i].X += velocities[i].X;
            positions[i].Y += velocities[i].Y;
            positions[i].Z += velocities[i].Z;
        }
    }
}

[DisassemblyDiagnoser(exportDiff: true, exportHtml: true, printSource: true, exportCombinedDisassemblyReport: true)]
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class CapabilityAccessBenchmark
{
    private delegate void ProcessDelegate(Span<Position> positions, ReadOnlySpan<Velocity> velocities);

    [Params(1000, 10000, 100000)]
    public int EntityCount { get; set; }

    [Params(1, 10)]
    public int ChunkCount { get; set; }

    private Position[] _positions = null!;
    private Velocity[] _velocities = null!;
    private IComponentAccessor _boxedAccessor = null!;
    private ProcessDelegate _cachedDelegate = null!;

    [GlobalSetup]
    public void Setup()
    {
        _positions = new Position[EntityCount];
        _velocities = new Velocity[EntityCount];

        var rng = new Random(42);
        for (int i = 0; i < EntityCount; i++)
        {
            _positions[i] = new Position { X = rng.NextSingle(), Y = rng.NextSingle(), Z = rng.NextSingle() };
            _velocities[i] = new Velocity { X = rng.NextSingle(), Y = rng.NextSingle(), Z = rng.NextSingle() };
        }

        _boxedAccessor = new ArchetypeAccessor();

        var method = typeof(CapabilityAccessBenchmark)
            .GetMethod(nameof(RunGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(typeof(ArchetypeAccessor));
        _cachedDelegate = method.CreateDelegate<ProcessDelegate>();
    }

    private static void RunGeneric<T>(Span<Position> positions, ReadOnlySpan<Velocity> velocities) where T : struct, IComponentAccessor
    {
        var accessor = default(T);
        accessor.Process(positions, velocities);
    }

    [Benchmark(Baseline = true)]
    public void Direct()
    {
        int chunkSize = EntityCount / ChunkCount;
        var accessor = new ArchetypeAccessor();

        for (int i = 0; i < ChunkCount; i++)
        {
            int offset = i * chunkSize;
            int length = i == ChunkCount - 1 ? EntityCount - offset : chunkSize;
            accessor.Process(_positions.AsSpan(offset, length), _velocities.AsSpan(offset, length));
        }
    }

    [Benchmark]
    public void InterfaceDispatch()
    {
        int chunkSize = EntityCount / ChunkCount;

        for (int i = 0; i < ChunkCount; i++)
        {
            int offset = i * chunkSize;
            int length = i == ChunkCount - 1 ? EntityCount - offset : chunkSize;
            _boxedAccessor.Process(_positions.AsSpan(offset, length), _velocities.AsSpan(offset, length));
        }
    }

    [Benchmark]
    public void StructConstrainedGeneric()
    {
        int chunkSize = EntityCount / ChunkCount;

        for (int i = 0; i < ChunkCount; i++)
        {
            int offset = i * chunkSize;
            int length = i == ChunkCount - 1 ? EntityCount - offset : chunkSize;
            RunGeneric<ArchetypeAccessor>(_positions.AsSpan(offset, length), _velocities.AsSpan(offset, length));
        }
    }

    [Benchmark]
    public void RuntimeGenericSpecialization()
    {
        int chunkSize = EntityCount / ChunkCount;

        for (int i = 0; i < ChunkCount; i++)
        {
            int offset = i * chunkSize;
            int length = i == ChunkCount - 1 ? EntityCount - offset : chunkSize;
            _cachedDelegate(_positions.AsSpan(offset, length), _velocities.AsSpan(offset, length));
        }
    }
}
