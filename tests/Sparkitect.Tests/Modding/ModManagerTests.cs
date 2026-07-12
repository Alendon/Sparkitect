using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Semver;
using Sparkitect.Modding;
using Sparkitect.Utils;

namespace Sparkitect.Tests.Modding;

/// <summary>
/// Covers <see cref="ModManager.BuildModLoadOrder"/>: the topo-order over a group's required
/// (non-optional, non-incompatible) mod-dependency edges that feeds the per-mod ALC chain. Reached via
/// <c>InternalsVisibleTo("Sparkitect.Tests")</c> — see <see cref="Sparkitect.Modding.ModManager"/>'s
/// internal static method. Sort mechanics themselves (tiebreak, cycle/missing-dependency diagnostics,
/// self-edge/parallel-edge handling) are covered once in
/// <see cref="Sparkitect.Tests.Utils.Ordering.OrderingGraphBuilderTests"/> and not re-tested here — these
/// tests only assert ModManager's translation of manifests into nodes/edges.
/// </summary>
public class ModManagerTests
{
    private static ModManifest Manifest(string id, params ModRelationship[] relationships) => new(
        Id: id,
        Name: id,
        Description: "",
        Version: SemVersion.Parse("1.0.0", SemVersionStyles.Any),
        Authors: [],
        ModPath: null,
        Relationships: relationships,
        ModAssembly: $"{id}.dll",
        RequiredAssemblies: []);

    private static ModRelationship Required(string id) => new(id, SemVersionRange.All);

    [Test]
    public async Task Chain_BRequiresA_CRequiresB_OrdersDependencyBeforeDependent()
    {
        var manifests = new[]
        {
            Manifest("C", Required("B")),
            Manifest("A"),
            Manifest("B", Required("A"))
        };

        var order = ModManager.BuildModLoadOrder(manifests);

        await Assert.That(order).IsEquivalentTo(new[] { "A", "B", "C" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task NoRelationships_OrdersDeterministicallyByLexicographicId_RegardlessOfInsertionOrder()
    {
        var manifests = new[]
        {
            Manifest("mod_c"),
            Manifest("mod_a"),
            Manifest("mod_b")
        };

        var order = ModManager.BuildModLoadOrder(manifests);

        await Assert.That(order).IsEquivalentTo(new[] { "mod_a", "mod_b", "mod_c" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task RequiredDependencyOutsideGroup_EdgeDroppedSilently_InGroupModsStillOrder()
    {
        // "outside_dep" is required by "mod_a" but has no manifest in this group (it's already loaded in
        // a parent group/layer) — must not error, and the in-group chain must still order correctly.
        var manifests = new[]
        {
            Manifest("mod_b", Required("mod_a")),
            Manifest("mod_a", Required("outside_dep"))
        };

        var order = ModManager.BuildModLoadOrder(manifests);

        await Assert.That(order).DoesNotContain("outside_dep");
        await Assert.That(order).IsEquivalentTo(new[] { "mod_a", "mod_b" }, CollectionOrdering.Matching);
    }

    [Test]
    public async Task DependencyCycle_Throws()
    {
        var manifests = new[]
        {
            Manifest("A", Required("B")),
            Manifest("B", Required("A"))
        };

        await Assert.That(() => ModManager.BuildModLoadOrder(manifests)).Throws<InvalidOperationException>();
    }
}

/// <summary>
/// Covers <see cref="ModManager.LoadModDependencies"/>: a mod's <see cref="ModManifest.RequiredAssemblies"/>
/// are hard dependencies, unlike <see cref="ModRelationship.IsOptional"/> mod-to-mod relationships. Both
/// failure paths (entry absent from the archive; entry present but fails to load) must stop the load at
/// origin rather than warn/log and continue silently — mirrors the existing throw shape in
/// <see cref="ModManager.LoadModAssembly"/>, its sibling for the primary mod assembly.
/// </summary>
public class ModManagerLoadDependenciesTests
{
    private static ModManifest Manifest(string id, params string[] requiredAssemblies) => new(
        Id: id,
        Name: id,
        Description: "",
        Version: SemVersion.Parse("1.0.0", SemVersionStyles.Any),
        Authors: [],
        ModPath: null,
        Relationships: [],
        ModAssembly: $"{id}.dll",
        RequiredAssemblies: requiredAssemblies);

    private static ZipArchive EmptyArchive(MemoryStream backing)
    {
        using (var writer = new ZipArchive(backing, ZipArchiveMode.Create, leaveOpen: true))
        {
            // No entries — the required assembly is absent from the archive.
        }
        backing.Seek(0, SeekOrigin.Begin);
        return new ZipArchive(backing, ZipArchiveMode.Read);
    }

    private static ZipArchive ArchiveWithCorruptEntry(MemoryStream backing, string entryPath)
    {
        using (var writer = new ZipArchive(backing, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = writer.CreateEntry(entryPath);
            using var entryStream = entry.Open();
            // Not a valid PE/metadata image — ModuleMetadata.CreateFromStream must fail on this.
            byte[] garbage = [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07];
            entryStream.Write(garbage, 0, garbage.Length);
        }
        backing.Seek(0, SeekOrigin.Begin);
        return new ZipArchive(backing, ZipArchiveMode.Read);
    }

    [Test]
    public async Task MissingRequiredAssemblyEntry_ThrowsInsteadOfWarningAndContinuing()
    {
        using var backing = new MemoryStream();
        using var archive = EmptyArchive(backing);
        var manifest = Manifest("mod_missing_dep", "missing.dll");
        var loadContext = new SparkitectLoadContext(null);

        await Assert.That(() =>
                ModManager.LoadModDependencies(manifest, archive, "mod_missing_dep", loadContext))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task RequiredAssemblyFailsToLoad_PropagatesExceptionInsteadOfLoggingAndContinuing()
    {
        using var backing = new MemoryStream();
        using var archive = ArchiveWithCorruptEntry(backing, "lib/corrupt.dll");
        var manifest = Manifest("mod_corrupt_dep", "corrupt.dll");
        var loadContext = new SparkitectLoadContext(null);

        await Assert.That(() =>
                ModManager.LoadModDependencies(manifest, archive, "mod_corrupt_dep", loadContext))
            .Throws<Exception>();
    }
}

/// <summary>
/// Covers <see cref="ModManager.DrainUnload"/> (shared bounded drain loop) and
/// <see cref="ModManager.ClassifyUnloadOutcomes"/> (per-mod leak attribution) against
/// synthetic collectible <see cref="AssemblyLoadContext"/>s. Real mod-DLL unload is proven by the
/// manual Release/no-debugger sample-mod run — these tests assert only the drain loop's shape (drives a
/// released context to !IsAlive; returns without throwing when exhausted against a still-rooted target) and
/// the classification helper's per-context (never group-wide) attribution. No Serilog assertions, no
/// mod-loading integration harness.
/// </summary>
public class ModManagerDrainUnloadTests
{
    [Test]
    public async Task DrainUnload_ReleasedCollectibleContext_DrivesWeakReferenceToDead()
    {
        var weakRef = CreateAndUnloadCollectibleContext();

        ModManager.DrainUnload(weakRef, ModManager.UnloadDrainIterationCap);

        await Assert.That(weakRef.IsAlive).IsFalse();
    }

    [Test]
    public async Task DrainUnload_StillRootedTarget_ReturnsWithoutThrowing_RemainsAlive()
    {
        var stillRootedContext = new AssemblyLoadContext(
            nameof(DrainUnload_StillRootedTarget_ReturnsWithoutThrowing_RemainsAlive), isCollectible: true);
        var weakRef = new WeakReference(stillRootedContext);

        await Assert.That(() => ModManager.DrainUnload(weakRef, ModManager.UnloadDrainIterationCap))
            .ThrowsNothing();

        await Assert.That(weakRef.IsAlive).IsTrue();
        GC.KeepAlive(stillRootedContext);
    }

    [Test]
    public async Task ClassifyUnloadOutcomes_PerContextAttribution_NamesOnlyThePinnedModAsLeaked()
    {
        var cleanContextRef = CreateAndUnloadCollectibleContext();
        ModManager.DrainUnload(cleanContextRef, ModManager.UnloadDrainIterationCap);

        var pinnedContext = new AssemblyLoadContext(
            nameof(ClassifyUnloadOutcomes_PerContextAttribution_NamesOnlyThePinnedModAsLeaked),
            isCollectible: true);
        var leakedContextRef = new WeakReference(pinnedContext);

        var captured = new[]
        {
            new ModManager.CapturedUnload(cleanContextRef, ["a"]),
            new ModManager.CapturedUnload(leakedContextRef, ["b"])
        };

        var (clean, leaked) = ModManager.ClassifyUnloadOutcomes(captured);

        await Assert.That(clean).IsEquivalentTo(new[] { "a" }, CollectionOrdering.Matching);
        await Assert.That(leaked).IsEquivalentTo(new[] { "b" }, CollectionOrdering.Matching);
        GC.KeepAlive(pinnedContext);
    }

    [Test]
    public async Task ClassifyUnloadOutcomes_VirtualEngineMod_NeverNamedInEitherVerdict()
    {
        var cleanContextRef = CreateAndUnloadCollectibleContext();
        ModManager.DrainUnload(cleanContextRef, ModManager.UnloadDrainIterationCap);

        var pinnedContext = new AssemblyLoadContext(
            nameof(ClassifyUnloadOutcomes_VirtualEngineMod_NeverNamedInEitherVerdict),
            isCollectible: true);
        var leakedContextRef = new WeakReference(pinnedContext);

        var captured = new[]
        {
            new ModManager.CapturedUnload(cleanContextRef, [Constants.VirtualSparkitectModId, "a"]),
            new ModManager.CapturedUnload(leakedContextRef, [Constants.VirtualSparkitectModId, "b"])
        };

        var (clean, leaked) = ModManager.ClassifyUnloadOutcomes(captured);

        await Assert.That(clean).IsEquivalentTo(new[] { "a" }, CollectionOrdering.Matching);
        await Assert.That(leaked).IsEquivalentTo(new[] { "b" }, CollectionOrdering.Matching);
        GC.KeepAlive(pinnedContext);
    }

    // Isolated in its own NoInlining helper so the caller's stack frame cannot pin the collectible context
    // being released (Pitfall 2) — returns only a WeakReference, the canonical Microsoft-documented shape.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateAndUnloadCollectibleContext()
    {
        var alc = new AssemblyLoadContext(nameof(CreateAndUnloadCollectibleContext), isCollectible: true);
        var weakRef = new WeakReference(alc);
        alc.Unload();
        return weakRef;
    }
}
