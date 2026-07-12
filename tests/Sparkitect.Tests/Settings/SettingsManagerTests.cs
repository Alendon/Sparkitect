using Sparkitect.Modding;
using Sparkitect.Settings;
using Sparkitect.Utils.DU;

namespace Sparkitect.Tests.Settings;

/// <summary>
/// A synthetic in-test setting source: preset acquisition values plus optional ordering metadata.
/// Writable sources persist writes into their own in-memory dictionary (sources own their acquisition).
/// </summary>
internal sealed class StubSource : ISettingSource
{
    private readonly Dictionary<Identification, object> _values;

    public StubSource(
        string sourceId,
        bool canWrite,
        Dictionary<Identification, object>? values = null,
        IReadOnlyList<SettingSourceOrder>? orderBefore = null,
        IReadOnlyList<SettingSourceOrder>? orderAfter = null)
    {
        SourceId = sourceId;
        CanWrite = canWrite;
        _values = values ?? new Dictionary<Identification, object>();
        OrderBefore = orderBefore ?? [];
        OrderAfter = orderAfter ?? [];
    }

    public string SourceId { get; }
    public bool CanWrite { get; }
    public IReadOnlyList<SettingSourceOrder> OrderBefore { get; }
    public IReadOnlyList<SettingSourceOrder> OrderAfter { get; }

    public bool TryGet(Identification id, out object? value) => _values.TryGetValue(id, out value);

    public Result<SetError> Write(Identification id, object? value)
    {
        if (!CanWrite)
        {
            return new SetError.SourceReadonly(Identification.Empty);
        }

        _values[id] = value!;
        return new Result<SetError>.Ok();
    }
}

public class SettingsManagerTests
{
    private static readonly Identification<bool> Flag = new(Identification.Create(100, 1, 1));
    private static readonly Identification UserSourceId = Identification.Create(100, 2, 1);
    private static readonly Identification HighSourceId = Identification.Create(100, 2, 2);
    private static readonly Identification LowSourceId = Identification.Create(100, 2, 3);
    private static readonly Identification ReadonlySourceId = Identification.Create(100, 2, 4);

    [Test]
    public async Task DeclaredSetting_NoSource_ReturnsDefault()
    {
        var manager = new SettingsManager();
        manager.Declare(Flag, new SettingDefinition<bool>(Default: true));

        await Assert.That(manager.GetValue<bool>(Flag)).IsTrue();
        await Assert.That(manager.GetSetting<bool>(Flag).Value).IsTrue();
    }

    [Test]
    public async Task HigherSource_ShadowsLower()
    {
        var manager = new SettingsManager();
        manager.Declare(Flag, new SettingDefinition<bool>(Default: false));

        // Low registered first, then high ordered before it (higher precedence shadows lower).
        manager.RegisterSource(LowSourceId, new StubSource("low", canWrite: false,
            values: new Dictionary<Identification, object> { [Flag] = false }));
        manager.RegisterSource(HighSourceId, new StubSource("high", canWrite: false,
            values: new Dictionary<Identification, object> { [Flag] = true },
            orderBefore: [new SettingSourceOrder(LowSourceId)]));
        manager.ProcessRegisteredSources();

        await Assert.That(manager.GetValue<bool>(Flag)).IsTrue();
    }

    [Test]
    public async Task WriteToUserSource_OverridesDeclaredDefault()
    {
        // Mod-override proxy (ROADMAP Success Criterion #5): per D-10 there is no mod-override source;
        // a mod overrides another mod's default via a later-ordered Entrypoint that imperatively calls
        // this same write primitive. The "later-ordered entrypoint runs after the target" half is owned
        // and tested by the Entrypoint system + the 57.1 ordering core, not re-proven here.
        var manager = new SettingsManager();
        manager.Declare(Flag, new SettingDefinition<bool>(Default: false));
        manager.RegisterSource(UserSourceId, new StubSource("user", canWrite: true));
        manager.ProcessRegisteredSources();

        var setting = manager.GetSetting<bool>(Flag);
        var result = setting.Set(true);

        await Assert.That(result is Result<SetError>.Ok).IsTrue();
        await Assert.That(setting.Value).IsTrue();
    }

    [Test]
    public async Task Set_ReadonlySource_ReturnsSourceReadonlyError()
    {
        var manager = new SettingsManager();
        manager.Declare(Flag, new SettingDefinition<bool>(Default: false));
        manager.RegisterSource(ReadonlySourceId, new StubSource("engine", canWrite: false));
        manager.ProcessRegisteredSources();

        var result = manager.Set(Flag, ReadonlySourceId, true);

        await Assert.That(result is Result<SetError>.Error { Value: SetError.SourceReadonly }).IsTrue();
        await Assert.That(manager.GetValue<bool>(Flag)).IsFalse();
    }

    [Test]
    public async Task Set_UnknownSetting_ReturnsUnknownSettingError()
    {
        var manager = new SettingsManager();
        manager.RegisterSource(UserSourceId, new StubSource("user", canWrite: true));
        manager.ProcessRegisteredSources();

        var result = manager.Set(Flag, UserSourceId, true);

        await Assert.That(result is Result<SetError>.Error { Value: SetError.UnknownSetting }).IsTrue();
    }

    [Test]
    public async Task Set_UnknownSource_ReturnsUnknownSourceError()
    {
        var manager = new SettingsManager();
        manager.Declare(Flag, new SettingDefinition<bool>(Default: false));

        var result = manager.Set(Flag, UserSourceId, true);

        await Assert.That(result is Result<SetError>.Error { Value: SetError.UnknownSource }).IsTrue();
    }

    [Test]
    public async Task Set_EffectiveChange_FiresCallbackOnceWithNewValue()
    {
        var manager = new SettingsManager();
        manager.Declare(Flag, new SettingDefinition<bool>(Default: false));
        manager.RegisterSource(UserSourceId, new StubSource("user", canWrite: true));
        manager.ProcessRegisteredSources();

        var fires = 0;
        var observed = false;
        manager.Subscribe<bool>(Flag, value =>
        {
            fires++;
            observed = value;
        });

        manager.Set(Flag, UserSourceId, true);

        await Assert.That(fires).IsEqualTo(1);
        await Assert.That(observed).IsTrue();
    }

    [Test]
    public async Task Set_ShadowedSource_FiresNoCallback()
    {
        var manager = new SettingsManager();
        manager.Declare(Flag, new SettingDefinition<bool>(Default: false));

        // User writable, shadowed by a higher readonly source that always supplies true.
        manager.RegisterSource(UserSourceId, new StubSource("user", canWrite: true));
        manager.RegisterSource(HighSourceId, new StubSource("high", canWrite: false,
            values: new Dictionary<Identification, object> { [Flag] = true },
            orderBefore: [new SettingSourceOrder(UserSourceId)]));
        manager.ProcessRegisteredSources();

        var fires = 0;
        manager.Subscribe<bool>(Flag, _ => fires++);

        // Writing to the shadowed user source cannot change the effective (high-source) value.
        manager.Set(Flag, UserSourceId, false);

        await Assert.That(fires).IsEqualTo(0);
    }

    [Test]
    public async Task ClearSubscriptionsForFrame_StopsDispatch()
    {
        var manager = new SettingsManager();
        var frameToken = new object();
        manager.UseFrameTokenProvider(() => frameToken);
        manager.Declare(Flag, new SettingDefinition<bool>(Default: false));
        manager.RegisterSource(UserSourceId, new StubSource("user", canWrite: true));
        manager.ProcessRegisteredSources();

        var fires = 0;
        manager.Subscribe<bool>(Flag, _ => fires++);
        manager.ClearSubscriptionsForFrame(frameToken);

        manager.Set(Flag, UserSourceId, true);

        await Assert.That(fires).IsEqualTo(0);
    }

    // F-03 regression: setting-source teardown must remove the exact source, recompute the effective
    // order, and notify subscribers when removal changes the resolved value. Previously the generated
    // teardown path had no removal effect at all -- the source lingered after "unregistration".

    [Test]
    public async Task RemoveSource_EffectiveSource_FallsBackToNextOrDefault()
    {
        var manager = new SettingsManager();
        manager.Declare(Flag, new SettingDefinition<bool>(Default: false));
        manager.RegisterSource(UserSourceId, new StubSource("user", canWrite: true,
            values: new Dictionary<Identification, object> { [Flag] = true }));
        manager.ProcessRegisteredSources();

        await Assert.That(manager.GetValue<bool>(Flag)).IsTrue();

        manager.RemoveSource(UserSourceId);

        await Assert.That(manager.GetValue<bool>(Flag)).IsFalse();
    }

    [Test]
    public async Task RemoveSource_ReRegister_Oscillation_SingleCurrentInstance()
    {
        var manager = new SettingsManager();
        manager.Declare(Flag, new SettingDefinition<bool>(Default: false));
        manager.RegisterSource(UserSourceId, new StubSource("first", canWrite: true,
            values: new Dictionary<Identification, object> { [Flag] = true }));
        manager.ProcessRegisteredSources();

        manager.RemoveSource(UserSourceId);
        await Assert.That(manager.GetValue<bool>(Flag)).IsFalse();

        // Reactivation must resolve through the new instance, not any lingering registration.
        manager.RegisterSource(UserSourceId, new StubSource("second", canWrite: true,
            values: new Dictionary<Identification, object> { [Flag] = true }));
        manager.ProcessRegisteredSources();

        await Assert.That(manager.GetValue<bool>(Flag)).IsTrue();

        var result = manager.SetUserValue(Flag, false);
        await Assert.That(result is Result<SetError>.Ok).IsTrue();
        await Assert.That(manager.GetValue<bool>(Flag)).IsFalse();
    }

    [Test]
    public async Task RemoveSource_EffectiveValueChanges_FiresCallback()
    {
        var manager = new SettingsManager();
        manager.Declare(Flag, new SettingDefinition<bool>(Default: false));
        manager.RegisterSource(UserSourceId, new StubSource("user", canWrite: true,
            values: new Dictionary<Identification, object> { [Flag] = true }));
        manager.ProcessRegisteredSources();

        var fires = 0;
        var observed = true;
        manager.Subscribe<bool>(Flag, value =>
        {
            fires++;
            observed = value;
        });

        manager.RemoveSource(UserSourceId);

        await Assert.That(fires).IsEqualTo(1);
        await Assert.That(observed).IsFalse();
    }

    [Test]
    public async Task RemoveSource_ShadowedSource_FiresNoCallback()
    {
        var manager = new SettingsManager();
        manager.Declare(Flag, new SettingDefinition<bool>(Default: false));

        // Low registered, shadowed by a higher source that always supplies the effective value. The
        // ordering edge lives on the removed (low) source itself, so removal never leaves a dangling
        // required-edge reference on the source that stays registered.
        manager.RegisterSource(HighSourceId, new StubSource("high", canWrite: false,
            values: new Dictionary<Identification, object> { [Flag] = true }));
        manager.RegisterSource(LowSourceId, new StubSource("low", canWrite: false,
            values: new Dictionary<Identification, object> { [Flag] = true },
            orderAfter: [new SettingSourceOrder(HighSourceId)]));
        manager.ProcessRegisteredSources();

        var fires = 0;
        manager.Subscribe<bool>(Flag, _ => fires++);

        // Removing the shadowed (non-effective) source cannot change the resolved value.
        manager.RemoveSource(LowSourceId);

        await Assert.That(fires).IsEqualTo(0);
    }

    [Test]
    public async Task RemoveSource_UserSource_ClearsUserSourcePointer()
    {
        var manager = new SettingsManager();
        manager.Declare(Flag, new SettingDefinition<bool>(Default: false));
        manager.RegisterSource(UserSourceId, new StubSource("user", canWrite: true));
        manager.ProcessRegisteredSources();

        manager.RemoveSource(UserSourceId);

        var result = manager.SetUserValue(Flag, true);
        await Assert.That(result is Result<SetError>.Error { Value: SetError.UnknownSource }).IsTrue();
    }

    [Test]
    public async Task RemoveSource_UnknownSource_IsNoOp()
    {
        var manager = new SettingsManager();
        manager.Declare(Flag, new SettingDefinition<bool>(Default: false));
        manager.RegisterSource(UserSourceId, new StubSource("user", canWrite: true,
            values: new Dictionary<Identification, object> { [Flag] = true }));
        manager.ProcessRegisteredSources();

        await Assert.That(() => manager.RemoveSource(HighSourceId)).ThrowsNothing();
        await Assert.That(manager.GetValue<bool>(Flag)).IsTrue();
    }
}
