namespace Sparkitect.Tests;

public class Sample
{
    [Test]
    public async Task TrueEqualsTrue()
    {
        var value = true;
        await Assert.That(value).IsTrue();
    }
}