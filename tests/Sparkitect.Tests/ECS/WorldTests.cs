using Sparkitect.ECS;
using Sparkitect.ECS.Capabilities;
using Sparkitect.ECS.Storage;
using TUnit.Assertions.Extensions;

namespace Sparkitect.Tests.ECS;

#region Test Helpers

public interface ITestCapability : ICapability;

public record TestCapabilityMetadata(string Name) : ICapabilityMetadata;

public class TestCapabilityRequirement : ICapabilityRequirement<TestCapabilityMetadata>
{
    private readonly string _name;

    public TestCapabilityRequirement(string name)
    {
        _name = name;
    }

    public bool Matches(TestCapabilityMetadata metadata)
    {
        return metadata.Name == _name;
    }
}

public class TestStorage : IStorage<int>
{
    private int _nextKey;
    public bool IsDisposed { get; private set; }

    public int AllocateEntity() => _nextKey++;

    public void Dispose()
    {
        IsDisposed = true;
    }
}

public class TestCapabilityStorage : IStorage<int>, ITestCapability
{
    private int _nextKey;
    public bool IsDisposed { get; private set; }

    public int AllocateEntity() => _nextKey++;

    public void Dispose()
    {
        IsDisposed = true;
    }
}

public class ThrowingStorage : IStorage<int>
{
    public bool IsDisposed { get; private set; }

    public int AllocateEntity() => 0;

    public void Dispose()
    {
        IsDisposed = true;
        throw new InvalidOperationException("Dispose failed on purpose");
    }
}

#endregion

public class WorldTests
{
    #region AddStorage / GetStorage

    [Test]
    public async Task AddStorage_ReturnsValidHandle()
    {
        using var world = new World();
        var storage = new TestStorage();

        var handle = world.AddStorage(storage, Array.Empty<CapabilityRegistration>());

        await Assert.That(handle.Generation).IsGreaterThanOrEqualTo((uint)0);
    }

    [Test]
    public async Task AddStorage_MultipleStorages_ReturnsDifferentHandles()
    {
        using var world = new World();
        var s1 = new TestStorage();
        var s2 = new TestStorage();

        var h1 = world.AddStorage(s1, Array.Empty<CapabilityRegistration>());
        var h2 = world.AddStorage(s2, Array.Empty<CapabilityRegistration>());

        await Assert.That(h1).IsNotEqualTo(h2);
    }

    [Test]
    public async Task GetStorage_ValidHandle_ReturnsAccessorWithCorrectHandle()
    {
        using var world = new World();
        var storage = new TestStorage();
        var handle = world.AddStorage(storage, Array.Empty<CapabilityRegistration>());

        var accessor = world.GetStorage(handle);

        await Assert.That(accessor.Handle).IsEqualTo(handle);
    }

    [Test]
    public async Task GetStorage_ValidHandle_AccessorCanCastToStorageType()
    {
        using var world = new World();
        var storage = new TestCapabilityStorage();
        var handle = world.AddStorage(storage, Array.Empty<CapabilityRegistration>());

        var accessor = world.GetStorage(handle);
        var cap = accessor.As<ITestCapability>();

        await Assert.That(cap).IsNotNull();
    }

    [Test]
    public async Task GetStorage_StaleHandle_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var storage = new TestStorage();
        var handle = world.AddStorage(storage, Array.Empty<CapabilityRegistration>());
        world.RemoveStorage(handle);

        await Assert.That(() =>
        {
            _ = world.GetStorage(handle);
        }).Throws<InvalidOperationException>();
    }

    #endregion

    #region RemoveStorage

    [Test]
    public async Task RemoveStorage_DisposesStorage()
    {
        using var world = new World();
        var storage = new TestStorage();
        var handle = world.AddStorage(storage, Array.Empty<CapabilityRegistration>());

        world.RemoveStorage(handle);

        await Assert.That(storage.IsDisposed).IsTrue();
    }

    [Test]
    public async Task RemoveStorage_InvalidatesHandle()
    {
        using var world = new World();
        var storage = new TestStorage();
        var handle = world.AddStorage(storage, Array.Empty<CapabilityRegistration>());

        world.RemoveStorage(handle);

        await Assert.That(() =>
        {
            _ = world.GetStorage(handle);
        }).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task RemoveStorage_StaleHandle_ThrowsInvalidOperationException()
    {
        using var world = new World();
        var storage = new TestStorage();
        var handle = world.AddStorage(storage, Array.Empty<CapabilityRegistration>());

        world.RemoveStorage(handle);

        await Assert.That(() => world.RemoveStorage(handle)).Throws<InvalidOperationException>();
    }

    #endregion

    #region Resolve

    [Test]
    public async Task Resolve_MatchingFilter_ReturnsMatchingHandles()
    {
        using var world = new World();
        var storage = new TestCapabilityStorage();
        var capabilities = new[]
        {
            new CapabilityRegistration
            {
                CapabilityType = typeof(ITestCapability),
                Metadata = new TestCapabilityMetadata("Position")
            }
        };
        var handle = world.AddStorage(storage, capabilities);

        var results = world.Resolve(new ICapabilityRequirement[]
        {
            new TestCapabilityRequirement("Position")
        });

        await Assert.That(results).HasCount().EqualTo(1);
        await Assert.That(results[0]).IsEqualTo(handle);
    }

    [Test]
    public async Task Resolve_NonMatchingFilter_ReturnsEmpty()
    {
        using var world = new World();
        var storage = new TestCapabilityStorage();
        var capabilities = new[]
        {
            new CapabilityRegistration
            {
                CapabilityType = typeof(ITestCapability),
                Metadata = new TestCapabilityMetadata("Position")
            }
        };
        world.AddStorage(storage, capabilities);

        var results = world.Resolve(new ICapabilityRequirement[]
        {
            new TestCapabilityRequirement("Velocity")
        });

        await Assert.That(results).HasCount().EqualTo(0);
    }

    [Test]
    public async Task Resolve_MultipleRequirements_AllMustMatch()
    {
        using var world = new World();
        var storage = new TestCapabilityStorage();
        var capabilities = new[]
        {
            new CapabilityRegistration
            {
                CapabilityType = typeof(ITestCapability),
                Metadata = new TestCapabilityMetadata("Position")
            }
        };
        world.AddStorage(storage, capabilities);

        // Requires both Position and Velocity, but storage only has Position
        var results = world.Resolve(new ICapabilityRequirement[]
        {
            new TestCapabilityRequirement("Position"),
            new TestCapabilityRequirement("Velocity")
        });

        await Assert.That(results).HasCount().EqualTo(0);
    }

    [Test]
    public async Task AddStorage_WithCapabilities_MakesStorageDiscoverableViaResolve()
    {
        using var world = new World();
        var s1 = new TestCapabilityStorage();
        var s2 = new TestCapabilityStorage();

        world.AddStorage(s1, new[]
        {
            new CapabilityRegistration
            {
                CapabilityType = typeof(ITestCapability),
                Metadata = new TestCapabilityMetadata("Position")
            }
        });
        world.AddStorage(s2, new[]
        {
            new CapabilityRegistration
            {
                CapabilityType = typeof(ITestCapability),
                Metadata = new TestCapabilityMetadata("Velocity")
            }
        });

        var posResults = world.Resolve(new ICapabilityRequirement[]
        {
            new TestCapabilityRequirement("Position")
        });
        var velResults = world.Resolve(new ICapabilityRequirement[]
        {
            new TestCapabilityRequirement("Velocity")
        });

        await Assert.That(posResults).HasCount().EqualTo(1);
        await Assert.That(velResults).HasCount().EqualTo(1);
    }

    #endregion

    #region RegisterFilter / UnregisterFilter

    [Test]
    public async Task RegisterFilter_FiresCallbackImmediatelyWithCurrentMatches()
    {
        using var world = new World();
        var storage = new TestCapabilityStorage();
        world.AddStorage(storage, new[]
        {
            new CapabilityRegistration
            {
                CapabilityType = typeof(ITestCapability),
                Metadata = new TestCapabilityMetadata("Position")
            }
        });

        IReadOnlyList<StorageHandle>? received = null;
        world.RegisterFilter(
            new ICapabilityRequirement[] { new TestCapabilityRequirement("Position") },
            handles => received = handles);

        await Assert.That(received).IsNotNull();
        await Assert.That(received!).HasCount().EqualTo(1);
    }

    [Test]
    public async Task RegisterFilter_FiresCallbackWhenNewMatchingStorageAdded()
    {
        using var world = new World();
        var callbackCount = 0;
        IReadOnlyList<StorageHandle>? lastReceived = null;

        world.RegisterFilter(
            new ICapabilityRequirement[] { new TestCapabilityRequirement("Position") },
            handles =>
            {
                callbackCount++;
                lastReceived = handles;
            });

        // First callback fires immediately with empty set
        await Assert.That(callbackCount).IsEqualTo(1);
        await Assert.That(lastReceived!).HasCount().EqualTo(0);

        // Add matching storage
        var storage = new TestCapabilityStorage();
        world.AddStorage(storage, new[]
        {
            new CapabilityRegistration
            {
                CapabilityType = typeof(ITestCapability),
                Metadata = new TestCapabilityMetadata("Position")
            }
        });

        await Assert.That(callbackCount).IsEqualTo(2);
        await Assert.That(lastReceived!).HasCount().EqualTo(1);
    }

    [Test]
    public async Task RegisterFilter_FiresCallbackWhenMatchingStorageRemoved()
    {
        using var world = new World();
        var callbackCount = 0;
        IReadOnlyList<StorageHandle>? lastReceived = null;

        var storage = new TestCapabilityStorage();
        var handle = world.AddStorage(storage, new[]
        {
            new CapabilityRegistration
            {
                CapabilityType = typeof(ITestCapability),
                Metadata = new TestCapabilityMetadata("Position")
            }
        });

        world.RegisterFilter(
            new ICapabilityRequirement[] { new TestCapabilityRequirement("Position") },
            handles =>
            {
                callbackCount++;
                lastReceived = handles;
            });

        // Initial callback with 1 match
        await Assert.That(callbackCount).IsEqualTo(1);
        await Assert.That(lastReceived!).HasCount().EqualTo(1);

        // Remove matching storage
        world.RemoveStorage(handle);

        await Assert.That(callbackCount).IsEqualTo(2);
        await Assert.That(lastReceived!).HasCount().EqualTo(0);
    }

    [Test]
    public async Task UnregisterFilter_StopsCallbackFromFiring()
    {
        using var world = new World();
        var callbackCount = 0;

        var filterHandle = world.RegisterFilter(
            new ICapabilityRequirement[] { new TestCapabilityRequirement("Position") },
            _ => callbackCount++);

        // Initial callback
        await Assert.That(callbackCount).IsEqualTo(1);

        world.UnregisterFilter(filterHandle);

        // Add matching storage -- callback should NOT fire
        var storage = new TestCapabilityStorage();
        world.AddStorage(storage, new[]
        {
            new CapabilityRegistration
            {
                CapabilityType = typeof(ITestCapability),
                Metadata = new TestCapabilityMetadata("Position")
            }
        });

        await Assert.That(callbackCount).IsEqualTo(1);
    }

    #endregion

    #region Dispose

    [Test]
    public async Task Dispose_DisposesAllActiveStorages()
    {
        var s1 = new TestStorage();
        var s2 = new TestStorage();

        var world = new World();
        world.AddStorage(s1, Array.Empty<CapabilityRegistration>());
        world.AddStorage(s2, Array.Empty<CapabilityRegistration>());

        world.Dispose();

        await Assert.That(s1.IsDisposed).IsTrue();
        await Assert.That(s2.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Dispose_HandlesPerItemExceptions()
    {
        var throwing = new ThrowingStorage();
        var normal = new TestStorage();

        var world = new World();
        world.AddStorage(throwing, Array.Empty<CapabilityRegistration>());
        world.AddStorage(normal, Array.Empty<CapabilityRegistration>());

        // Should not throw even though one storage throws during dispose
        world.Dispose();

        await Assert.That(throwing.IsDisposed).IsTrue();
        await Assert.That(normal.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Dispose_MakesAllHandlesStale()
    {
        var world = new World();
        var storage = new TestStorage();
        var handle = world.AddStorage(storage, Array.Empty<CapabilityRegistration>());

        world.Dispose();

        await Assert.That(() =>
        {
            _ = world.GetStorage(handle);
        }).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Dispose_DoubleDispose_IsSafe()
    {
        var world = new World();
        var storage = new TestStorage();
        world.AddStorage(storage, Array.Empty<CapabilityRegistration>());

        world.Dispose();
        world.Dispose(); // Should not throw

        await Assert.That(storage.IsDisposed).IsTrue();
    }

    #endregion

    #region Slot Recycling

    [Test]
    public async Task SlotRecycling_FreedSlotIsReused()
    {
        using var world = new World();
        var s1 = new TestStorage();
        var handle1 = world.AddStorage(s1, Array.Empty<CapabilityRegistration>());
        var originalIndex = handle1.Index;

        world.RemoveStorage(handle1);

        var s2 = new TestStorage();
        var handle2 = world.AddStorage(s2, Array.Empty<CapabilityRegistration>());

        // Same slot index reused
        await Assert.That(handle2.Index).IsEqualTo(originalIndex);
        // But generation incremented
        await Assert.That(handle2.Generation).IsGreaterThan(handle1.Generation);
    }

    [Test]
    public async Task SlotRecycling_StaleHandleCannotAccessNewStorage()
    {
        using var world = new World();
        var s1 = new TestStorage();
        var staleHandle = world.AddStorage(s1, Array.Empty<CapabilityRegistration>());

        world.RemoveStorage(staleHandle);

        var s2 = new TestStorage();
        world.AddStorage(s2, Array.Empty<CapabilityRegistration>());

        // staleHandle has old generation, should fail
        await Assert.That(() =>
        {
            _ = world.GetStorage(staleHandle);
        }).Throws<InvalidOperationException>();
    }

    #endregion

    #region Array Growth

    [Test]
    public async Task ArrayGrowth_MoreThanInitialCapacity_WorksCorrectly()
    {
        using var world = new World();
        var handles = new List<StorageHandle>();

        // Add more than initial capacity (4)
        for (int i = 0; i < 10; i++)
        {
            var storage = new TestStorage();
            var handle = world.AddStorage(storage, Array.Empty<CapabilityRegistration>());
            handles.Add(handle);
        }

        // All handles should be valid
        for (int i = 0; i < 10; i++)
        {
            var accessor = world.GetStorage(handles[i]);
            await Assert.That(accessor.Handle).IsEqualTo(handles[i]);
        }
    }

    #endregion
}
