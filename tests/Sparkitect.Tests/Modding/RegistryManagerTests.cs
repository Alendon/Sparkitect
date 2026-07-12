using System.Diagnostics.CodeAnalysis;
using Imposter.Abstractions;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.Resolution;
using Sparkitect.GameState;
using Sparkitect.Modding;

[assembly: GenerateImposter(typeof(IResourceManager))]

namespace Sparkitect.Tests.Modding;

/// <summary>
/// F-02 regression: registry category lifecycle must survive add -> remove -> add oscillation without a
/// duplicate-category collision, and teardown must fail loudly if entries were not fully reversed first.
/// Drives the real <see cref="RegistryManager"/> add/remove path rather than asserting registry storage.
/// </summary>
public class RegistryManagerTests
{
    private static readonly Identification TestModuleId = Identification.Create(1, 1, 1);

    private sealed class TestModule : IHasIdentification, IStateModule
    {
        public static Identification Identification => TestModuleId;
        public IReadOnlyList<Identification> Requires => [];
        public IReadOnlyList<Identification> ActivatesWith => [];
    }

    private sealed class TestRegistry : IRegistry<TestModule>
    {
        public const string RegistryIdentifier = "test:oscillation_registry";

        public static string Identifier => RegistryIdentifier;
        public static Identification OwningModule => TestModuleId;
        public void Unregister(Identification id)
        {
        }
    }

    private sealed class FakeFactoryContainer(IReadOnlyDictionary<string, IRegistryBase> instances)
        : IFactoryContainer<string, IRegistryBase>
    {
        public IReadOnlyDictionary<string, IRegistryBase> ResolveAll() => instances;

        public bool TryResolve(string key, [NotNullWhen(true)] out IRegistryBase? instance) =>
            instances.TryGetValue(key, out instance);

        public void Dispose()
        {
        }
    }

    // Hand-written rather than Imposter-generated: IModManager.LoadMods takes a `params
    // ReadOnlySpan<ModFileIdentifier>`, and Imposter's generic Arg<T> matcher cannot hold a ref struct.
    private sealed class FakeModManager : IModManager
    {
        IReadOnlyCollection<ModFileIdentifier> IModManager.LoadedMods => [];
        IReadOnlyList<IReadOnlyList<ModFileIdentifier>> IModManager.LoadedModsPerGroup => [];
        public IReadOnlyList<ModManifest> DiscoveredArchives => [];
        public void DiscoverMods()
        {
        }

        void IModManager.LoadMods(params ReadOnlySpan<ModFileIdentifier> identifiers)
        {
        }

        IReadOnlyList<ModFileIdentifier> IModManager.UnloadLastModGroup() => [];
    }

    private static (IRegistryLifecycleManager Lifecycle, IIdentificationManager Identification, ICoreContainer Container)
        CreateManager()
    {
        var identificationManager = new IdentificationManager();
        var registry = new TestRegistry();
        var factoryContainer = new FakeFactoryContainer(
            new Dictionary<string, IRegistryBase> { [TestRegistry.RegistryIdentifier] = registry });

        var diServiceImposter = new IDIServiceImposter(ImposterMode.Implicit);
        diServiceImposter.BuildFactoryContainer<string, IRegistryBase>(
                Arg<ICoreContainer>.Any(), Arg<IResolutionProvider?>.Any(), Arg<IEnumerable<string>>.Any(),
                Arg<Type>.Any(), Arg<bool>.Any())
            .Returns(factoryContainer);

        var containerImposter = new ICoreContainerImposter(ImposterMode.Implicit);
        var container = containerImposter.Instance();

        var gsmImposter = new IGameStateManagerImposter(ImposterMode.Implicit);
        gsmImposter.CurrentCoreContainer.Getter().Returns(container);

        var resourceManagerImposter = new IResourceManagerImposter(ImposterMode.Implicit);

        var manager = new RegistryManager
        {
            ModManager = new FakeModManager(),
            IdentificationManager = identificationManager,
            GameStateManager = gsmImposter.Instance(),
            DIService = diServiceImposter.Instance(),
            ResourceManager = resourceManagerImposter.Instance(),
        };

        return (manager, identificationManager, container);
    }

    [Test]
    public async Task AddRemoveAdd_Oscillation_DoesNotCollideOnCategory()
    {
        var (lifecycle, identification, container) = CreateManager();

        lifecycle.AddModuleRegistries(TestModuleId, container);
        await Assert.That(identification.IsCategoryRegistered(TestRegistry.RegistryIdentifier)).IsTrue();

        lifecycle.RemoveModuleRegistries(TestModuleId);
        await Assert.That(identification.IsCategoryRegistered(TestRegistry.RegistryIdentifier)).IsFalse();

        // Re-add must not throw "Category already registered" -- this is the F-02 regression: a removed
        // module registry used to retain its category, colliding on reactivation.
        await Assert.That(() => lifecycle.AddModuleRegistries(TestModuleId, container)).ThrowsNothing();
        await Assert.That(identification.IsCategoryRegistered(TestRegistry.RegistryIdentifier)).IsTrue();
    }

    [Test]
    public async Task RemoveModuleRegistries_EntriesNotReversed_ThrowsInvalidOperationException()
    {
        var (lifecycle, identification, container) = CreateManager();

        lifecycle.AddModuleRegistries(TestModuleId, container);

        // Simulate a teardown bug: an object registered under the category was never reversed by the
        // module-driven exit path before the registry itself is torn down.
        identification.RegisterMod("test_mod");
        identification.RegisterObject("test_mod", TestRegistry.RegistryIdentifier, "leftover");

        await Assert.That(() => lifecycle.RemoveModuleRegistries(TestModuleId))
            .Throws<InvalidOperationException>();
    }
}
