using System.Numerics;
using Moq;
using Silk.NET.Input;
using Sparkitect.Windowing.Input;

namespace Sparkitect.Tests.Windowing.Input;

public class SparkitMouseTests
{
    [Test]
    public async Task GetPosition_Always_ReturnsSilkMousePosition()
    {
        // Arrange
        var mockMouse = new Mock<IMouse>();
        mockMouse.SetupGet(m => m.Position).Returns(new Vector2(100, 200));
        var sparkitMouse = new SparkitMouse(mockMouse.Object);

        // Act
        var position = sparkitMouse.GetPosition();

        // Assert
        await Assert.That(position).IsEqualTo(new Vector2(100, 200));
    }

    [Test]
    public async Task GetDelta_WhenFocused_ReturnsCalculatedDelta()
    {
        // Arrange
        var mockMouse = new Mock<IMouse>();
        mockMouse.SetupGet(m => m.Position).Returns(new Vector2(0, 0));
        var sparkitMouse = new SparkitMouse(mockMouse.Object);

        sparkitMouse.UpdateDelta(); // Initial position captured
        mockMouse.SetupGet(m => m.Position).Returns(new Vector2(10, 20));
        sparkitMouse.UpdateDelta(); // Delta calculated

        // Act
        var delta = sparkitMouse.GetDelta();

        // Assert
        await Assert.That(delta).IsEqualTo(new Vector2(10, 20));
    }

    [Test]
    public async Task GetDelta_WhenUnfocused_Released_ReturnsZero()
    {
        // Arrange
        var mockMouse = new Mock<IMouse>();
        mockMouse.SetupGet(m => m.Position).Returns(new Vector2(0, 0));
        var sparkitMouse = new SparkitMouse(mockMouse.Object);

        sparkitMouse.UpdateDelta();
        mockMouse.SetupGet(m => m.Position).Returns(new Vector2(10, 20));
        sparkitMouse.UpdateDelta();
        sparkitMouse.SetFocusBehavior(FocusLostBehavior.Released);
        sparkitMouse.SetFocusState(false);

        // Act
        var delta = sparkitMouse.GetDelta();

        // Assert
        await Assert.That(delta).IsEqualTo(Vector2.Zero);
    }

    [Test]
    public async Task GetDelta_WhenUnfocused_Frozen_ReturnsDelta()
    {
        // Arrange
        var mockMouse = new Mock<IMouse>();
        mockMouse.SetupGet(m => m.Position).Returns(new Vector2(0, 0));
        var sparkitMouse = new SparkitMouse(mockMouse.Object);

        sparkitMouse.UpdateDelta();
        mockMouse.SetupGet(m => m.Position).Returns(new Vector2(10, 20));
        sparkitMouse.UpdateDelta();
        sparkitMouse.SetFocusBehavior(FocusLostBehavior.Frozen);
        sparkitMouse.SetFocusState(false);

        // Act
        var delta = sparkitMouse.GetDelta();

        // Assert
        await Assert.That(delta).IsEqualTo(new Vector2(10, 20));
    }

    [Test]
    public async Task IsButtonDown_WhenFocused_DelegatesToSilk()
    {
        // Arrange
        var mockMouse = new Mock<IMouse>();
        mockMouse.SetupGet(m => m.Position).Returns(Vector2.Zero);
        mockMouse.Setup(m => m.IsButtonPressed(MouseButton.Left)).Returns(true);
        var sparkitMouse = new SparkitMouse(mockMouse.Object);

        // Act
        var result = sparkitMouse.IsButtonDown(MouseButton.Left);

        // Assert
        await Assert.That(result).IsTrue();
        mockMouse.Verify(m => m.IsButtonPressed(MouseButton.Left), Times.Once);
    }

    [Test]
    public async Task IsButtonDown_WhenUnfocused_Released_ReturnsFalse()
    {
        // Arrange
        var mockMouse = new Mock<IMouse>();
        mockMouse.SetupGet(m => m.Position).Returns(Vector2.Zero);
        mockMouse.Setup(m => m.IsButtonPressed(MouseButton.Left)).Returns(true);
        var sparkitMouse = new SparkitMouse(mockMouse.Object);

        sparkitMouse.SetFocusBehavior(FocusLostBehavior.Released);
        sparkitMouse.SetFocusState(false);

        // Act
        var result = sparkitMouse.IsButtonDown(MouseButton.Left);

        // Assert
        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsButtonDown_WhenUnfocused_Frozen_DelegatesToSilk()
    {
        // Arrange
        var mockMouse = new Mock<IMouse>();
        mockMouse.SetupGet(m => m.Position).Returns(Vector2.Zero);
        mockMouse.Setup(m => m.IsButtonPressed(MouseButton.Left)).Returns(true);
        var sparkitMouse = new SparkitMouse(mockMouse.Object);

        sparkitMouse.SetFocusBehavior(FocusLostBehavior.Frozen);
        sparkitMouse.SetFocusState(false);

        // Act
        var result = sparkitMouse.IsButtonDown(MouseButton.Left);

        // Assert
        await Assert.That(result).IsTrue();
        mockMouse.Verify(m => m.IsButtonPressed(MouseButton.Left), Times.Once);
    }

    [Test]
    public async Task SetFocusState_RegainFocus_ResetsDeltaTracking()
    {
        // Arrange
        var mockMouse = new Mock<IMouse>();
        mockMouse.SetupGet(m => m.Position).Returns(new Vector2(0, 0));
        var sparkitMouse = new SparkitMouse(mockMouse.Object);

        sparkitMouse.UpdateDelta(); // Initial position captured
        mockMouse.SetupGet(m => m.Position).Returns(new Vector2(500, 500)); // Big move while unfocused
        sparkitMouse.SetFocusState(false); // Lost focus
        sparkitMouse.SetFocusState(true);  // Regain focus - should reset tracking

        // Act
        sparkitMouse.UpdateDelta();
        var delta = sparkitMouse.GetDelta();

        // Assert - delta should NOT be (500, 500), it should be (0, 0) or small
        await Assert.That(delta).IsEqualTo(Vector2.Zero);
    }
}
