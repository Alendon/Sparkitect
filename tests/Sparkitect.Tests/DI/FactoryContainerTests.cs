using Moq;
using Sparkitect.DI;
using Sparkitect.DI.Container;

namespace Sparkitect.Tests.DI;

public class FactoryContainerTests
{
    private static IKeyedFactory<ITestService> CreateMockFactory(string key, ITestService instance, Type? implementationType = null)
    {
        var mock = new Mock<IKeyedFactory<ITestService>>();
        mock.Setup(f => f.Key).Returns(key);
        mock.Setup(f => f.CreateInstance()).Returns(instance);
        mock.Setup(f => f.ImplementationType).Returns(implementationType ?? instance.GetType());
        return mock.Object;
    }

    private static IKeyedFactory<ITestService> CreateDisposableMockFactory(string key, ITestService instance, Action onDispose)
    {
        var mock = new Mock<IKeyedFactory<ITestService>>();
        mock.As<IDisposable>().Setup(d => d.Dispose()).Callback(onDispose);
        mock.Setup(f => f.Key).Returns(key);
        mock.Setup(f => f.CreateInstance()).Returns(instance);
        mock.Setup(f => f.ImplementationType).Returns(instance.GetType());
        return mock.Object;
    }

    [Test]
    public async Task TryResolve_WithStringKey_ReturnsInstance()
    {
        // Arrange
        var testService = new TestService();
        var factory = CreateMockFactory("test-key", testService);
        var factories = new Dictionary<string, IKeyedFactory<ITestService>>
        {
            { "test-key", factory }
        };
        var container = new FactoryContainer<ITestService>(factories);

        // Act
        var result = container.TryResolve("test-key", out var instance);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(instance).IsNotNull();
        await Assert.That(instance).IsEqualTo(testService);
    }

    [Test]
    public async Task TryResolve_WithUnregisteredKey_ReturnsFalse()
    {
        // Arrange
        var testService = new TestService();
        var factory = CreateMockFactory("existing-key", testService);
        var factories = new Dictionary<string, IKeyedFactory<ITestService>>
        {
            { "existing-key", factory }
        };
        var container = new FactoryContainer<ITestService>(factories);

        // Act
        var result = container.TryResolve("non-existent-key", out var instance);

        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(instance).IsNull();
    }

    [Test]
    public async Task TryResolve_AfterDispose_ReturnsFalse()
    {
        // Arrange
        var testService = new TestService();
        var factory = CreateMockFactory("test-key", testService);
        var factories = new Dictionary<string, IKeyedFactory<ITestService>>
        {
            { "test-key", factory }
        };
        var container = new FactoryContainer<ITestService>(factories);
        container.Dispose();

        // Act
        var result = container.TryResolve("test-key", out var instance);

        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(instance).IsNull();
    }

    [Test]
    public async Task ResolveAll_WithMultipleFactories_ReturnsAllInstances()
    {
        // Arrange
        var service1 = new TestService();
        var service2 = new OverrideTestService();
        var factory1 = CreateMockFactory("key1", service1);
        var factory2 = CreateMockFactory("key2", service2);
        var factories = new Dictionary<string, IKeyedFactory<ITestService>>
        {
            { "key1", factory1 },
            { "key2", factory2 }
        };
        var container = new FactoryContainer<ITestService>(factories);

        // Act
        var result = container.ResolveAll();

        // Assert
        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(2);
        await Assert.That(result["key1"]).IsEqualTo(service1);
        await Assert.That(result["key2"]).IsEqualTo(service2);
    }

    [Test]
    public async Task ResolveAll_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var testService = new TestService();
        var factory = CreateMockFactory("test-key", testService);
        var factories = new Dictionary<string, IKeyedFactory<ITestService>>
        {
            { "test-key", factory }
        };
        var container = new FactoryContainer<ITestService>(factories);
        container.Dispose();

        // Act & Assert
        await Assert.That(() => container.ResolveAll()).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task Dispose_WithDisposableFactories_DisposesAll()
    {
        // Arrange
        var disposed1 = false;
        var disposed2 = false;
        var factory1 = CreateDisposableMockFactory("key1", new TestService(), () => disposed1 = true);
        var factory2 = CreateDisposableMockFactory("key2", new TestService(), () => disposed2 = true);
        var factories = new Dictionary<string, IKeyedFactory<ITestService>>
        {
            { "key1", factory1 },
            { "key2", factory2 }
        };
        var container = new FactoryContainer<ITestService>(factories);

        // Act
        container.Dispose();

        // Assert
        await Assert.That(disposed1).IsTrue();
        await Assert.That(disposed2).IsTrue();
    }
}
