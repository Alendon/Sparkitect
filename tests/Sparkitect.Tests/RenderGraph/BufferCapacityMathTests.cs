using Sparkitect.Graphics.RenderGraph.Resources;

namespace Sparkitect.Tests.RenderGraph;

public class BufferCapacityMathTests
{
    [Test]
    public async Task NextCapacity_NeededFitsCurrent_ReturnsCurrent()
    {
        await Assert.That(BufferCapacity.NextCapacity(256, 100)).IsEqualTo(256ul);
    }

    [Test]
    public async Task NextCapacity_NeededEqualsCurrent_ReturnsCurrent()
    {
        await Assert.That(BufferCapacity.NextCapacity(16, 16)).IsEqualTo(16ul);
    }

    [Test]
    public async Task NextCapacity_NeededZero_ReturnsCurrent()
    {
        await Assert.That(BufferCapacity.NextCapacity(512, 0)).IsEqualTo(512ul);
    }

    [Test]
    public async Task NextCapacity_NonPowerNeeded_RoundsUpToNextPowerOfTwo()
    {
        await Assert.That(BufferCapacity.NextCapacity(256, 257)).IsEqualTo(512ul);
    }

    [Test]
    public async Task NextCapacity_GrowFromZeroCurrent_RoundsUpFromNeeded()
    {
        await Assert.That(BufferCapacity.NextCapacity(0, 300)).IsEqualTo(512ul);
    }

    [Test]
    public async Task NextCapacity_NeverShrinks_BelowCurrent()
    {
        // needed rounds to 512, but current already exceeds it: never shrink.
        await Assert.That(BufferCapacity.NextCapacity(1024, 300)).IsEqualTo(1024ul);
    }
}
