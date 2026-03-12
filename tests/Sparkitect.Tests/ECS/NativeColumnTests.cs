using Sparkitect.ECS.Storage;
using Sparkitect.Utils;

namespace Sparkitect.Tests.ECS;

/// <summary>
/// Fake object tracker for testing NativeColumn lifecycle.
/// </summary>
public class FakeObjectTracker : IObjectTracker<IDisposable>
{
    public int TrackCount { get; private set; }
    public int UntrackCount { get; private set; }
    private readonly List<IDisposable> _tracked = [];

    public int Count => _tracked.Count;

    public IObjectTracker<IDisposable>.Handle Track(IDisposable obj)
    {
        TrackCount++;
        _tracked.Add(obj);
        return new IObjectTracker<IDisposable>.Handle(this, obj);
    }

    public IObjectTracker<IDisposable>.Handle Track(IDisposable obj, CallerContext callsite)
    {
        return Track(obj);
    }

    public void Untrack(IDisposable obj)
    {
        UntrackCount++;
        _tracked.Remove(obj);
    }

    public ICollection<IDisposable> GetTracked() => _tracked;

    public IEnumerable<(IDisposable Object, CallerContext Callsite)> GetTrackingEntries()
    {
        return _tracked.Select(t => (t, default(CallerContext)));
    }

    public void DumpToLog(string context = "") { }
}

public class NativeColumnTests
{
    [Test]
    public async Task Constructor_AllocatesMemory_StartsWithZeroCount()
    {
        var tracker = new FakeObjectTracker();
        using var column = new NativeColumn(sizeof(int), sizeof(int), 8, tracker);

        await Assert.That(column.Count).IsEqualTo(0);
        await Assert.That(column.Capacity).IsEqualTo(8);
        await Assert.That(column.ElementSize).IsEqualTo(sizeof(int));
        await Assert.That(tracker.TrackCount).IsEqualTo(1);
    }

    [Test]
    public async Task SetGet_StoresAndRetrievesValue()
    {
        var tracker = new FakeObjectTracker();
        using var column = new NativeColumn(sizeof(int), sizeof(int), 8, tracker);

        var slot = column.AddSlot();
        column.Set(slot, 42);
        var value = column.Get<int>(slot);

        await Assert.That(value).IsEqualTo(42);
    }

    [Test]
    public async Task SetGet_MultipleSlots_IndependentValues()
    {
        var tracker = new FakeObjectTracker();
        using var column = new NativeColumn(sizeof(float), sizeof(float), 8, tracker);

        var s0 = column.AddSlot();
        var s1 = column.AddSlot();
        column.Set(s0, 1.5f);
        column.Set(s1, 2.5f);

        await Assert.That(column.Get<float>(s0)).IsEqualTo(1.5f);
        await Assert.That(column.Get<float>(s1)).IsEqualTo(2.5f);
    }

    [Test]
    public async Task EnsureCapacity_GrowsPreservingExistingData()
    {
        var tracker = new FakeObjectTracker();
        using var column = new NativeColumn(sizeof(int), sizeof(int), 2, tracker);

        var s0 = column.AddSlot();
        column.Set(s0, 100);
        var s1 = column.AddSlot();
        column.Set(s1, 200);

        // Adding a third slot triggers growth beyond initial capacity of 2
        var s2 = column.AddSlot();
        column.Set(s2, 300);

        await Assert.That(column.Capacity).IsGreaterThanOrEqualTo(3);
        await Assert.That(column.Get<int>(s0)).IsEqualTo(100);
        await Assert.That(column.Get<int>(s1)).IsEqualTo(200);
        await Assert.That(column.Get<int>(s2)).IsEqualTo(300);
    }

    [Test]
    public async Task SwapRemove_CopiesElementFromSourceToDestination()
    {
        var tracker = new FakeObjectTracker();
        using var column = new NativeColumn(sizeof(int), sizeof(int), 8, tracker);

        var s0 = column.AddSlot();
        var s1 = column.AddSlot();
        column.Set(s0, 10);
        column.Set(s1, 20);

        column.SwapRemove(0, 1); // copy from index 1 to index 0

        await Assert.That(column.Get<int>(0)).IsEqualTo(20);
    }

    [Test]
    public async Task Dispose_FreesMemoryAndCallsTrackerFree()
    {
        var tracker = new FakeObjectTracker();
        var column = new NativeColumn(sizeof(int), sizeof(int), 8, tracker);

        await Assert.That(tracker.TrackCount).IsEqualTo(1);
        await Assert.That(tracker.UntrackCount).IsEqualTo(0);

        column.Dispose();

        await Assert.That(tracker.UntrackCount).IsEqualTo(1);
    }

    [Test]
    public async Task Dispose_DoubleDispose_IsSafe()
    {
        var tracker = new FakeObjectTracker();
        var column = new NativeColumn(sizeof(int), sizeof(int), 8, tracker);

        column.Dispose();
        column.Dispose(); // Should not throw or double-free

        await Assert.That(tracker.UntrackCount).IsEqualTo(1);
    }

    [Test]
    public async Task AddSlot_ReturnsSequentialIndices()
    {
        var tracker = new FakeObjectTracker();
        using var column = new NativeColumn(sizeof(int), sizeof(int), 8, tracker);

        var s0 = column.AddSlot();
        var s1 = column.AddSlot();
        var s2 = column.AddSlot();

        await Assert.That(s0).IsEqualTo(0);
        await Assert.That(s1).IsEqualTo(1);
        await Assert.That(s2).IsEqualTo(2);
        await Assert.That(column.Count).IsEqualTo(3);
    }

    [Test]
    public async Task RemoveSlotBySwap_MiddleElement_SwapsWithLastAndDecrements()
    {
        var tracker = new FakeObjectTracker();
        using var column = new NativeColumn(sizeof(int), sizeof(int), 8, tracker);

        column.AddSlot(); column.Set(0, 10);
        column.AddSlot(); column.Set(1, 20);
        column.AddSlot(); column.Set(2, 30);

        column.RemoveSlotBySwap(0); // Remove first, last (30) moves to index 0

        await Assert.That(column.Count).IsEqualTo(2);
        await Assert.That(column.Get<int>(0)).IsEqualTo(30);
        await Assert.That(column.Get<int>(1)).IsEqualTo(20);
    }

    [Test]
    public async Task RemoveSlotBySwap_LastElement_JustDecrements()
    {
        var tracker = new FakeObjectTracker();
        using var column = new NativeColumn(sizeof(int), sizeof(int), 8, tracker);

        column.AddSlot(); column.Set(0, 10);
        column.AddSlot(); column.Set(1, 20);

        column.RemoveSlotBySwap(1); // Remove last, no swap needed

        await Assert.That(column.Count).IsEqualTo(1);
        await Assert.That(column.Get<int>(0)).IsEqualTo(10);
    }
}
