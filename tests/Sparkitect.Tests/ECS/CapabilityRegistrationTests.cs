using Sparkitect.ECS.Capabilities;

namespace Sparkitect.Tests.ECS;

public interface ICapabilityA : ICapability;

public interface ICapabilityB : ICapability;

public record SharedMetadata(string Tag) : ICapabilityMetadata;

public class RequirementA : ICapabilityRequirement<ICapabilityA, SharedMetadata>
{
    private readonly string _tag;

    public RequirementA(string tag)
    {
        _tag = tag;
    }

    public bool Matches(SharedMetadata metadata)
    {
        return metadata.Tag == _tag;
    }
}

public class RequirementB : ICapabilityRequirement<ICapabilityB, SharedMetadata>
{
    private readonly string _tag;

    public RequirementB(string tag)
    {
        _tag = tag;
    }

    public bool Matches(SharedMetadata metadata)
    {
        return metadata.Tag == _tag;
    }
}

public class CapabilityRegistrationTests
{
    [Test]
    public async Task TwoRegistrations_SameMetaType_DifferentCapability_DoNotCollide()
    {
        var regA = new CapabilityRegistration<ICapabilityA, SharedMetadata>(new SharedMetadata("pos"));
        var regB = new CapabilityRegistration<ICapabilityB, SharedMetadata>(new SharedMetadata("pos"));

        var reqA = new RequirementA("pos");
        var reqB = new RequirementB("pos");

        // regA should match reqA but NOT reqB
        await Assert.That(regA.TryMatch(reqA)).IsTrue();
        await Assert.That(regA.TryMatch(reqB)).IsFalse();

        // regB should match reqB but NOT reqA
        await Assert.That(regB.TryMatch(reqB)).IsTrue();
        await Assert.That(regB.TryMatch(reqA)).IsFalse();
    }

    [Test]
    public async Task CapabilityType_ReturnsCorrectInterfaceType()
    {
        ICapabilityRequirement reqA = new RequirementA("x");
        ICapabilityRequirement reqB = new RequirementB("x");

        await Assert.That(reqA.CapabilityType).IsEqualTo(typeof(ICapabilityA));
        await Assert.That(reqB.CapabilityType).IsEqualTo(typeof(ICapabilityB));
    }

    [Test]
    public async Task Registration_DoesNotMatch_WrongCapabilityType_SameMetaType()
    {
        var regA = new CapabilityRegistration<ICapabilityA, SharedMetadata>(new SharedMetadata("vel"));
        var reqB = new RequirementB("vel");

        await Assert.That(regA.TryMatch(reqB)).IsFalse();
    }
}
