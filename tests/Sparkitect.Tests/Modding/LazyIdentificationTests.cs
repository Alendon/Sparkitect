using Sparkitect.Modding;

namespace Sparkitect.Tests.Modding;

public class LazyIdentificationTests
{
    private class TestIdentifiable : IHasIdentification
    {
        public static Identification Identification => Sparkitect.Modding.Identification.Create(42, 7, 99);
    }

    // Stands in for the fail-loud accessor that throws when a target is unregistered / torn down.
    private sealed class ThrowingIdentifiable : IHasIdentification
    {
        public static Identification Identification => throw new InvalidOperationException("target unregistered");
    }

    [Test]
    public async Task Of_ReturnsCorrectIdentification()
    {
        var result = LazyIdentification.Of<TestIdentifiable>().Resolve();

        await Assert.That(result).IsEqualTo(IdentificationHelper.Read<TestIdentifiable>());
    }

    [Test]
    public async Task Resolve_CalledMultipleTimes_ReturnsSameValue()
    {
        var lazy = LazyIdentification.Of<TestIdentifiable>();

        var first = lazy.Resolve();
        var second = lazy.Resolve();

        await Assert.That(second).IsEqualTo(first);
    }

    [Test]
    public async Task Of_WithTestType_ResolvesToExpectedValue()
    {
        var result = LazyIdentification.Of<TestIdentifiable>().Resolve();

        await Assert.That(result).IsEqualTo(Identification.Create(42, 7, 99));
    }

    [Test]
    public async Task Resolve_TargetThrows_PropagatesException()
    {
        await Assert.That(() => LazyIdentification.Of<ThrowingIdentifiable>().Resolve())
            .Throws<InvalidOperationException>();
    }
}
