using Sparkitect.DI.Container;

namespace Sparkitect.Tests.DI;

public class EntrypointContainerTests
{
    [Test]
    public async Task ResolveMany_WithInstances_ReturnsReadOnlyList()
    {
        // Arrange
        var service1 = new EntrypointService();
        var service2 = new AnotherEntrypointService();
        var instances = new List<IEntrypointService> { service1, service2 };
        var container = new EntrypointContainer<IEntrypointService>(instances);

        // Act
        var result = container.ResolveMany();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result).IsTypeOf<IReadOnlyList<IEntrypointService>>();
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result[0]).IsEqualTo(service1);
        await Assert.That(result[1]).IsEqualTo(service2);
    }

    [Test]
    public async Task ResolveMany_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var instances = new List<IEntrypointService>();
        var container = new EntrypointContainer<IEntrypointService>(instances);

        // Act
        var result = container.ResolveMany();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ResolveMany_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var instances = new List<IEntrypointService> { new EntrypointService() };
        var container = new EntrypointContainer<IEntrypointService>(instances);
        container.Dispose();

        // Act & Assert
        await Assert.That(() => container.ResolveMany()).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task ProcessMany_WithInstances_InvokesActionOnEach()
    {
        // Arrange
        var service1 = new EntrypointService();
        var service2 = new AnotherEntrypointService();
        var instances = new List<IEntrypointService> { service1, service2 };
        var container = new EntrypointContainer<IEntrypointService>(instances);
        var processedItems = new List<IEntrypointService>();

        // Act
        container.ProcessMany(s => processedItems.Add(s));

        // Assert
        await Assert.That(processedItems.Count).IsEqualTo(2);
        await Assert.That(processedItems[0]).IsEqualTo(service1);
        await Assert.That(processedItems[1]).IsEqualTo(service2);
    }

    [Test]
    public async Task ProcessMany_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var instances = new List<IEntrypointService> { new EntrypointService() };
        var container = new EntrypointContainer<IEntrypointService>(instances);
        container.Dispose();

        // Act & Assert
        await Assert.That(() => container.ProcessMany(_ => { })).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task Dispose_WithDisposableInstances_DisposesAll()
    {
        // Arrange
        var disposable1 = new DisposableEntrypointService();
        var disposable2 = new DisposableEntrypointService();
        var instances = new List<IEntrypointService> { disposable1, disposable2 };
        var container = new EntrypointContainer<IEntrypointService>(instances);

        // Act
        container.Dispose();

        // Assert
        await Assert.That(disposable1.IsDisposed).IsTrue();
        await Assert.That(disposable2.IsDisposed).IsTrue();
    }

    [Test]
    public async Task Dispose_WithMixedInstances_OnlyDisposesDisposable()
    {
        // Arrange
        var nonDisposable = new EntrypointService();
        var disposable = new DisposableEntrypointService();
        var instances = new List<IEntrypointService> { nonDisposable, disposable };
        var container = new EntrypointContainer<IEntrypointService>(instances);

        // Act - dispose should complete without exception
        var exceptionThrown = false;
        try
        {
            container.Dispose();
        }
        catch
        {
            exceptionThrown = true;
        }

        // Assert - disposable was disposed, no exception for non-disposable
        await Assert.That(disposable.IsDisposed).IsTrue();
        await Assert.That(exceptionThrown).IsFalse();
    }

    [Test]
    public async Task Dispose_CalledTwice_NoException()
    {
        // Arrange
        var disposable = new DisposableEntrypointService();
        var instances = new List<IEntrypointService> { disposable };
        var container = new EntrypointContainer<IEntrypointService>(instances);

        // Act - call dispose twice
        container.Dispose();
        container.Dispose();

        // Assert - no exception thrown, disposable only disposed once
        await Assert.That(disposable.IsDisposed).IsTrue();
    }
}
