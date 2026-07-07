using System.Runtime.CompilerServices;
using Sparkitect.Modding;

namespace Sparkitect.Tests.Modding;

public class IdentificationTypedTests
{
    [Test]
    public async Task SizeOf_IsEightBytes()
    {
        await Assert.That(Unsafe.SizeOf<Identification<object>>()).IsEqualTo(8);
    }

    [Test]
    public async Task ImplicitConversion_RoundTripsToEqualBareIdentification()
    {
        var id = Identification.Create(1, 2, 3);
        var typed = new Identification<object>(id);

        Identification unwrapped = typed;

        await Assert.That(unwrapped).IsEqualTo(id);
    }

    [Test]
    public async Task Equals_SameInnerId_ReturnsTrueAndSharesHashCode()
    {
        var id = Identification.Create(5, 6, 7);
        var left = new Identification<object>(id);
        var right = new Identification<object>(id);

        await Assert.That(left == right).IsTrue();
        await Assert.That(left.Equals(right)).IsTrue();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
    }

    [Test]
    public async Task Equals_DifferentInnerId_ReturnsFalse()
    {
        var left = new Identification<object>(Identification.Create(1, 1, 1));
        var right = new Identification<object>(Identification.Create(2, 2, 2));

        await Assert.That(left == right).IsFalse();
        await Assert.That(left != right).IsTrue();
        await Assert.That(left.Equals(right)).IsFalse();
    }
}
