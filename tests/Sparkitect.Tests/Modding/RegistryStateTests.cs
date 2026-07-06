using Sparkitect.Modding;

namespace Sparkitect.Tests.Modding;

public class RegistryStateTests
{
    private static readonly Identification TestTarget = Identification.Create(1, 1, 1);

    [Test]
    public async Task InitialPhase_IsIdle()
    {
        // Arrange
        var state = new RegistryState();

        // Assert
        await Assert.That(state.CurrentPhase).IsEqualTo(RegistryPhase.Idle);
    }

    [Test]
    public async Task EnterPopulating_TransitionsToPopulating()
    {
        // Arrange
        var state = new RegistryState();

        // Act
        state.EnterPopulating();

        // Assert
        await Assert.That(state.CurrentPhase).IsEqualTo(RegistryPhase.Populating);
    }

    [Test]
    public async Task EnterPopulating_FromPopulating_IsNoOp()
    {
        // Arrange
        var state = new RegistryState();
        state.EnterPopulating();

        // Act — second call should not throw
        state.EnterPopulating();

        // Assert
        await Assert.That(state.CurrentPhase).IsEqualTo(RegistryPhase.Populating);
    }

    [Test]
    public async Task EnterTearingDown_TransitionsToTearingDown()
    {
        // Arrange
        var state = new RegistryState();

        // Act
        state.EnterTearingDown();

        // Assert
        await Assert.That(state.CurrentPhase).IsEqualTo(RegistryPhase.TearingDown);
    }

    [Test]
    public async Task EnterTearingDown_FromTearingDown_IsNoOp()
    {
        // Arrange
        var state = new RegistryState();
        state.EnterTearingDown();

        // Act — second call should not throw
        state.EnterTearingDown();

        // Assert
        await Assert.That(state.CurrentPhase).IsEqualTo(RegistryPhase.TearingDown);
    }

    [Test]
    public async Task EnterPopulating_FromTearingDown_Throws()
    {
        // Arrange
        var state = new RegistryState();
        state.EnterTearingDown();

        // Act + Assert
        await Assert.That(() => state.EnterPopulating()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task EnterTearingDown_FromPopulating_Throws()
    {
        // Arrange
        var state = new RegistryState();
        state.EnterPopulating();

        // Act + Assert
        await Assert.That(() => state.EnterTearingDown()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task MutationRequest_Allocate_InPopulating_Succeeds()
    {
        // Arrange
        var state = new RegistryState();
        state.EnterPopulating();

        // Act — should not throw
        state.MutationRequest(new RegistryOperation.Allocate("test_mod", "blocks"), TestTarget);
    }

    [Test]
    public async Task MutationRequest_Allocate_InIdle_Throws()
    {
        // Arrange
        var state = new RegistryState();

        // Act + Assert
        await Assert.That(() =>
            state.MutationRequest(new RegistryOperation.Allocate("test_mod", "blocks"), TestTarget)
        ).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task MutationRequest_Mutate_InPopulating_Succeeds()
    {
        // Arrange
        var state = new RegistryState();
        state.EnterPopulating();

        // Act — should not throw
        state.MutationRequest(new RegistryOperation.Mutate(TestTarget), TestTarget);
    }

    [Test]
    public async Task MutationRequest_Mutate_InTearingDown_Throws()
    {
        // Arrange
        var state = new RegistryState();
        state.EnterTearingDown();

        // Act + Assert
        await Assert.That(() =>
            state.MutationRequest(new RegistryOperation.Mutate(TestTarget), TestTarget)
        ).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task MutationRequest_Destroy_InTearingDown_Succeeds()
    {
        // Arrange
        var state = new RegistryState();
        state.EnterTearingDown();

        // Act — should not throw
        state.MutationRequest(new RegistryOperation.Destroy(TestTarget), TestTarget);
    }

    [Test]
    public async Task MutationRequest_Destroy_InPopulating_Throws()
    {
        // Arrange
        var state = new RegistryState();
        state.EnterPopulating();

        // Act + Assert
        await Assert.That(() =>
            state.MutationRequest(new RegistryOperation.Destroy(TestTarget), TestTarget)
        ).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ReturnToIdle_FromAnyPhase_ReturnsToIdle()
    {
        // Arrange
        var state = new RegistryState();
        state.EnterPopulating();

        // Act
        state.ReturnToIdle();

        // Assert
        await Assert.That(state.CurrentPhase).IsEqualTo(RegistryPhase.Idle);
    }
}
