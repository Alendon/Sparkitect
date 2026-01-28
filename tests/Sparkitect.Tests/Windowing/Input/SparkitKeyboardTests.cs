using Moq;
using Silk.NET.Input;
using Sparkitect.Windowing.Input;

namespace Sparkitect.Tests.Windowing.Input;

public class SparkitKeyboardTests
{
    [Test]
    public async Task IsKeyDown_WhenFocused_DelegatesToSilk()
    {
        // Arrange
        var mockKeyboard = new Mock<Silk.NET.Input.IKeyboard>();
        mockKeyboard.Setup(k => k.IsKeyPressed(Key.W)).Returns(true);
        var sparkitKeyboard = new SparkitKeyboard(mockKeyboard.Object);

        // Act
        var result = sparkitKeyboard.IsKeyDown(Key.W);

        // Assert
        await Assert.That(result).IsTrue();
        mockKeyboard.Verify(k => k.IsKeyPressed(Key.W), Times.Once);
    }

    [Test]
    public async Task IsKeyDown_WhenUnfocused_Released_ReturnsFalse()
    {
        // Arrange
        var mockKeyboard = new Mock<Silk.NET.Input.IKeyboard>();
        mockKeyboard.Setup(k => k.IsKeyPressed(Key.W)).Returns(true);
        var sparkitKeyboard = new SparkitKeyboard(mockKeyboard.Object);

        sparkitKeyboard.SetFocusBehavior(FocusLostBehavior.Released);
        sparkitKeyboard.SetFocusState(false);

        // Act
        var result = sparkitKeyboard.IsKeyDown(Key.W);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsKeyDown_WhenUnfocused_Frozen_DelegatesToSilk()
    {
        // Arrange
        var mockKeyboard = new Mock<Silk.NET.Input.IKeyboard>();
        mockKeyboard.Setup(k => k.IsKeyPressed(Key.W)).Returns(true);
        var sparkitKeyboard = new SparkitKeyboard(mockKeyboard.Object);

        sparkitKeyboard.SetFocusBehavior(FocusLostBehavior.Frozen);
        sparkitKeyboard.SetFocusState(false);

        // Act
        var result = sparkitKeyboard.IsKeyDown(Key.W);

        // Assert
        await Assert.That(result).IsTrue();
        mockKeyboard.Verify(k => k.IsKeyPressed(Key.W), Times.Once);
    }

    [Test]
    public async Task SetFocusBehavior_Changes_AffectsKeyReporting()
    {
        // Arrange
        var mockKeyboard = new Mock<Silk.NET.Input.IKeyboard>();
        mockKeyboard.Setup(k => k.IsKeyPressed(Key.W)).Returns(true);
        var sparkitKeyboard = new SparkitKeyboard(mockKeyboard.Object);

        sparkitKeyboard.SetFocusState(false);

        // Act & Assert - With Released behavior (default)
        sparkitKeyboard.SetFocusBehavior(FocusLostBehavior.Released);
        var releasedResult = sparkitKeyboard.IsKeyDown(Key.W);
        await Assert.That(releasedResult).IsFalse();

        // Act & Assert - With Frozen behavior
        sparkitKeyboard.SetFocusBehavior(FocusLostBehavior.Frozen);
        var frozenResult = sparkitKeyboard.IsKeyDown(Key.W);
        await Assert.That(frozenResult).IsTrue();
    }

    [Test]
    public async Task SetFocusState_MultipleCalls_TracksCorrectly()
    {
        // Arrange
        var mockKeyboard = new Mock<Silk.NET.Input.IKeyboard>();
        mockKeyboard.Setup(k => k.IsKeyPressed(Key.W)).Returns(true);
        var sparkitKeyboard = new SparkitKeyboard(mockKeyboard.Object);

        sparkitKeyboard.SetFocusBehavior(FocusLostBehavior.Released);

        // Act & Assert - Initially focused
        await Assert.That(sparkitKeyboard.IsKeyDown(Key.W)).IsTrue();

        // Lose focus
        sparkitKeyboard.SetFocusState(false);
        await Assert.That(sparkitKeyboard.IsKeyDown(Key.W)).IsFalse();

        // Regain focus
        sparkitKeyboard.SetFocusState(true);
        await Assert.That(sparkitKeyboard.IsKeyDown(Key.W)).IsTrue();

        // Lose focus again
        sparkitKeyboard.SetFocusState(false);
        await Assert.That(sparkitKeyboard.IsKeyDown(Key.W)).IsFalse();
    }
}
