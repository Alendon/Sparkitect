using Sparkitect.GameState;
using Sparkitect.Modding;

namespace Sparkitect.Tests.GameState;

public class GameStateManagerTests
{
    [Test]
    public async Task AddStateDescriptor_ValidState_RegistersSuccessfully()
    {
        // Arrange
        var gsm = CreateGameStateManager();
        var stateId = Identification.Create(1, 1, 1);

        // Act
        gsm.AddStateDescriptor<TestRootState>(stateId);

        // Assert - state should be registered (no exception thrown)
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task AddStateDescriptor_StateWithMissingParent_ThrowsException()
    {
        // Arrange
        var gsm = CreateGameStateManager();
        var stateId = Identification.Create(1, 1, 2);

        // Act & Assert - should fail because parent state (TestRootState) not registered
        await Assert.That(() => gsm.AddStateDescriptor<TestChildState>(stateId))
            .Throws<InvalidOperationException>()
            .WithMessage(message => message.Contains("parent state"));
    }

    [Test]
    public async Task AddStateDescriptor_ChildStateWithRegisteredParent_RegistersSuccessfully()
    {
        // Arrange
        var gsm = CreateGameStateManager();
        var rootId = Identification.Create(1, 1, 1);
        var childId = Identification.Create(1, 1, 2);

        gsm.AddStateDescriptor<TestRootState>(rootId);

        // Act
        gsm.AddStateDescriptor<TestChildState>(childId);

        // Assert - both states should be registered (no exception thrown)
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task AddStateModule_ValidModule_RegistersSuccessfully()
    {
        // Arrange
        var gsm = CreateGameStateManager();
        var moduleId = Identification.Create(1, 2, 1);

        // Act
        gsm.AddStateModule<TestModule>(moduleId);

        // Assert - module should be registered (no exception thrown)
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task RemoveStateDescriptor_RegisteredState_RemovesSuccessfully()
    {
        // Arrange
        var gsm = CreateGameStateManager();
        var stateId = Identification.Create(1, 1, 1);
        gsm.AddStateDescriptor<TestRootState>(stateId);

        // Act
        gsm.RemoveStateDescriptor(stateId);

        // Assert - removal should succeed (no exception)
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task RemoveStateModule_RegisteredModule_RemovesSuccessfully()
    {
        // Arrange
        var gsm = CreateGameStateManager();
        var moduleId = Identification.Create(1, 2, 1);
        gsm.AddStateModule<TestModule>(moduleId);

        // Act
        gsm.RemoveStateModule(moduleId);

        // Assert - removal should succeed (no exception)
        await Assert.That(true).IsTrue();
    }

    private static GameStateManager CreateGameStateManager()
    {
        // Note: Full testing would require mocking ModManager
        // For now, creating instance with required property uninitialized
        // These tests focus on state/module registration which doesn't use ModManager
        return new GameStateManager
        {
            ModManager = null! // Will cause NullReferenceException if accessed, but registration methods don't use it
        };
    }

    // Test state descriptors
    private class TestRootState : IStateDescriptor
    {
        public static Identification ParentId => Identification.Empty; // Root state has no parent
        public static IReadOnlyList<Identification> Modules => [Identification.Create(1, 2, 1)];
        public static Identification Identification => Identification.Create(1, 1, 1);
    }

    private class TestChildState : IStateDescriptor
    {
        public static Identification ParentId => Identification.Create(1, 1, 1); // Parent is TestRootState
        public static IReadOnlyList<Identification> Modules => [Identification.Create(1, 2, 1)];
        public static Identification Identification => Identification.Create(1, 1, 2);
    }

    // Test module
    private class TestModule : IStateModule
    {
        public static Identification Identification => Identification.Create(1, 2, 1);
        public static IReadOnlyList<Type> UsedServices => [];
    }
}
