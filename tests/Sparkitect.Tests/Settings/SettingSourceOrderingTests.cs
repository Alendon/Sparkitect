using Sparkitect.Modding;
using Sparkitect.Settings;

namespace Sparkitect.Tests.Settings;

/// <summary>
/// Asserts the translation of source OrderBefore/OrderAfter metadata into the shared 57.1 ordering core
/// produces the right resolved precedence, and that cycles / missing required sources fail loud naming
/// the participating source ids. Does not re-test the 57.1 sort mechanics (57.1 D-06 owns those).
/// </summary>
public class SettingSourceOrderingTests
{
    private static readonly Identification Setting = Identification.Create(101, 1, 1);
    private static readonly Identification SourceA = Identification.Create(101, 2, 1);
    private static readonly Identification SourceB = Identification.Create(101, 2, 2);

    [Test]
    public async Task OrderAfter_PlacesTargetAheadInResolvedPrecedence()
    {
        var manager = new SettingsManager();
        manager.Declare(Setting, new SettingDefinition<int>(Default: 0));

        // A supplies 1, B supplies 2; B is ordered after A, so A takes precedence and shadows B.
        manager.RegisterSource(SourceA, new StubSource("source_a", canWrite: false,
            values: new Dictionary<Identification, object> { [Setting] = 1 }));
        manager.RegisterSource(SourceB, new StubSource("source_b", canWrite: false,
            values: new Dictionary<Identification, object> { [Setting] = 2 },
            orderAfter: [new SettingSourceOrder(SourceA)]));
        manager.ProcessRegisteredSources();

        await Assert.That(manager.GetValue<int>(Setting)).IsEqualTo(1);
    }

    [Test]
    public async Task RequiredTarget_RegisteredLaterInSamePass_Resolves()
    {
        var manager = new SettingsManager();
        manager.Declare(Setting, new SettingDefinition<int>(Default: 0));

        // B's required edge targets A before A is registered; the deferred recompute makes this legal
        // as long as A arrives before the pass is processed.
        manager.RegisterSource(SourceB, new StubSource("source_b", canWrite: false,
            values: new Dictionary<Identification, object> { [Setting] = 2 },
            orderAfter: [new SettingSourceOrder(SourceA)]));
        manager.RegisterSource(SourceA, new StubSource("source_a", canWrite: false,
            values: new Dictionary<Identification, object> { [Setting] = 1 }));
        manager.ProcessRegisteredSources();

        await Assert.That(manager.GetValue<int>(Setting)).IsEqualTo(1);
    }

    [Test]
    public async Task Resolve_WithUnprocessedRegistrations_ProcessesLazily()
    {
        var manager = new SettingsManager();
        manager.Declare(Setting, new SettingDefinition<int>(Default: 0));

        // No explicit ProcessRegisteredSources: the first resolve plays back the recorded pass itself.
        manager.RegisterSource(SourceA, new StubSource("source_a", canWrite: false,
            values: new Dictionary<Identification, object> { [Setting] = 5 }));

        await Assert.That(manager.GetValue<int>(Setting)).IsEqualTo(5);
    }

    [Test]
    public async Task OrderingCycle_FailsLoud_NamingParticipatingSources()
    {
        var manager = new SettingsManager();
        manager.RegisterSource(SourceA, new StubSource("source_a", canWrite: false));

        // B declares both before and after A — a cycle A->B->A once both nodes exist.
        var cyclic = new StubSource("source_b", canWrite: false,
            orderBefore: [new SettingSourceOrder(SourceA)],
            orderAfter: [new SettingSourceOrder(SourceA)]);
        manager.RegisterSource(SourceB, cyclic);

        var exception = await Assert.That(() => manager.ProcessRegisteredSources())
            .Throws<InvalidOperationException>();

        await Assert.That(exception!.Message).Contains("source_a");
        await Assert.That(exception!.Message).Contains("source_b");
    }

    [Test]
    public async Task MissingRequiredSource_FailsLoud_NamingSources()
    {
        var manager = new SettingsManager();

        // B orders after A, but A is never registered and the reference is required.
        var dangling = new StubSource("source_b", canWrite: false,
            orderAfter: [new SettingSourceOrder(SourceA)]);
        manager.RegisterSource(SourceB, dangling);

        var exception = await Assert.That(() => manager.ProcessRegisteredSources())
            .Throws<InvalidOperationException>();

        await Assert.That(exception!.Message).Contains("source_b");
        await Assert.That(exception!.Message).Contains(SourceA.ToString());
    }

    [Test]
    public async Task OptionalMissingSource_IsDroppedSilently()
    {
        var manager = new SettingsManager();
        manager.Declare(Setting, new SettingDefinition<int>(Default: 7));

        // Optional reference to an unregistered source is dropped; registration succeeds.
        manager.RegisterSource(SourceB, new StubSource("source_b", canWrite: false,
            values: new Dictionary<Identification, object> { [Setting] = 3 },
            orderAfter: [new SettingSourceOrder(SourceA, Optional: true)]));
        manager.ProcessRegisteredSources();

        await Assert.That(manager.GetValue<int>(Setting)).IsEqualTo(3);
    }
}
