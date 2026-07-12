using Moq;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.Resolution;

namespace Sparkitect.Tests.DI;

public class FactoryContainerTests
{
    private static IKeyedFactory<ITestService> CreateMockFactory(ITestService instance, Type? implementationType = null)
    {
        var mock = new Mock<IKeyedFactory<ITestService>>();
        mock.Setup(f => f.CreateInstance()).Returns(instance);
        mock.Setup(f => f.ImplementationType).Returns(implementationType ?? instance.GetType());
        return mock.Object;
    }

    private static IKeyedFactory<ITestService> CreateDisposableMockFactory(ITestService instance, Action onDispose)
    {
        var mock = new Mock<IKeyedFactory<ITestService>>();
        mock.As<IDisposable>().Setup(d => d.Dispose()).Callback(onDispose);
        mock.Setup(f => f.CreateInstance()).Returns(instance);
        mock.Setup(f => f.ImplementationType).Returns(instance.GetType());
        return mock.Object;
    }

    private static IKeyedFactory<ITestService> CreateThrowingDisposableMockFactory(ITestService instance, Action onDisposeAttempted)
    {
        var mock = new Mock<IKeyedFactory<ITestService>>();
        mock.As<IDisposable>().Setup(d => d.Dispose()).Callback(() =>
        {
            onDisposeAttempted();
            throw new InvalidOperationException("Simulated disposal failure");
        });
        mock.Setup(f => f.CreateInstance()).Returns(instance);
        mock.Setup(f => f.ImplementationType).Returns(instance.GetType());
        return mock.Object;
    }

    [Test]
    public async Task TryResolve_WithStringKey_ReturnsInstance()
    {
        // Arrange
        var testService = new TestService();
        var factory = CreateMockFactory(testService);
        var factories = new Dictionary<string, IKeyedFactory<ITestService>>
        {
            { "test-key", factory }
        };
        var container = new FactoryContainer<string, ITestService>(factories);

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
        var factory = CreateMockFactory(testService);
        var factories = new Dictionary<string, IKeyedFactory<ITestService>>
        {
            { "existing-key", factory }
        };
        var container = new FactoryContainer<string, ITestService>(factories);

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
        var factory = CreateMockFactory(testService);
        var factories = new Dictionary<string, IKeyedFactory<ITestService>>
        {
            { "test-key", factory }
        };
        var container = new FactoryContainer<string, ITestService>(factories);
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
        var factory1 = CreateMockFactory(service1);
        var factory2 = CreateMockFactory(service2);
        var factories = new Dictionary<string, IKeyedFactory<ITestService>>
        {
            { "key1", factory1 },
            { "key2", factory2 }
        };
        var container = new FactoryContainer<string, ITestService>(factories);

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
        var factory = CreateMockFactory(testService);
        var factories = new Dictionary<string, IKeyedFactory<ITestService>>
        {
            { "test-key", factory }
        };
        var container = new FactoryContainer<string, ITestService>(factories);
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
        var factory1 = CreateDisposableMockFactory(new TestService(), () => disposed1 = true);
        var factory2 = CreateDisposableMockFactory(new TestService(), () => disposed2 = true);
        var factories = new Dictionary<string, IKeyedFactory<ITestService>>
        {
            { "key1", factory1 },
            { "key2", factory2 }
        };
        var container = new FactoryContainer<string, ITestService>(factories);

        // Act
        container.Dispose();

        // Assert
        await Assert.That(disposed1).IsTrue();
        await Assert.That(disposed2).IsTrue();
    }

    private static IKeyedFactory<ITestService> CreatePreparedMockFactory(ITestService instance)
    {
        var mock = new Mock<IKeyedFactory<ITestService>>();
        mock.Setup(f => f.CreateInstance()).Returns(instance);
        mock.Setup(f => f.ImplementationType).Returns(instance.GetType());
        mock.Setup(f => f.TryPrepare(It.IsAny<IResolutionScope>())).Returns(true);
        return mock.Object;
    }

    [Test]
    public async Task Build_PreparesAndReturnsContainer_WithAggregateMap()
    {
        // Arrange
        var service = new TestService();
        var factory = CreatePreparedMockFactory(service);
        var registrations = new Dictionary<string, IKeyedFactory<ITestService>>
        {
            { "key1", factory }
        };
        var scope = new Mock<IResolutionScope>().Object;
        var builder = new FactoryContainerBuilder<string, ITestService>();

        // Act
        using var container = builder.Build(registrations, scope);

        // Assert
        var resolved = container.TryResolve("key1", out var instance);
        await Assert.That(resolved).IsTrue();
        await Assert.That(instance).IsEqualTo(service);
    }

    [Test]
    public async Task Build_SkipsFactory_WhenPrepareFailsAndSkipMissing()
    {
        // Arrange
        var readyService = new TestService();
        var readyFactory = CreatePreparedMockFactory(readyService);

        var failingMock = new Mock<IKeyedFactory<ITestService>>();
        failingMock.Setup(f => f.ImplementationType).Returns(typeof(TestService));
        failingMock.Setup(f => f.TryPrepare(It.IsAny<IResolutionScope>())).Returns(false);

        var registrations = new Dictionary<string, IKeyedFactory<ITestService>>
        {
            { "ready", readyFactory },
            { "missing", failingMock.Object }
        };
        var scope = new Mock<IResolutionScope>().Object;
        var builder = new FactoryContainerBuilder<string, ITestService>();

        // Act
        using var container = builder.Build(registrations, scope, skipMissing: true);

        // Assert: ready factory survives, missing factory dropped
        await Assert.That(container.TryResolve("ready", out _)).IsTrue();
        await Assert.That(container.TryResolve("missing", out _)).IsFalse();
    }

    [Test]
    public async Task Build_Throws_WhenPrepareFailsAndSkipMissingFalse()
    {
        // Arrange
        var failingMock = new Mock<IKeyedFactory<ITestService>>();
        failingMock.Setup(f => f.ImplementationType).Returns(typeof(TestService));
        failingMock.Setup(f => f.TryPrepare(It.IsAny<IResolutionScope>())).Returns(false);

        var registrations = new Dictionary<string, IKeyedFactory<ITestService>>
        {
            { "missing", failingMock.Object }
        };
        var scope = new Mock<IResolutionScope>().Object;
        var builder = new FactoryContainerBuilder<string, ITestService>();

        // Act & Assert
        await Assert.That(() => builder.Build(registrations, scope, skipMissing: false))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task AggregateMap_LaterWins_WhenSameKeyWrittenTwice()
    {
        // Arrange: DIService-style orchestration — configurators write into a shared aggregate dict,
        // later writes override earlier writes for the same key.
        var firstService = new TestService();
        var secondService = new OverrideTestService();
        var firstFactory = CreatePreparedMockFactory(firstService);
        var secondFactory = CreatePreparedMockFactory(secondService);

        var registrations = new Dictionary<string, IKeyedFactory<ITestService>>();

        // Act: two configurators write into the aggregate; second wins on the shared key
        registrations["shared-key"] = firstFactory;
        registrations["shared-key"] = secondFactory;

        var scope = new Mock<IResolutionScope>().Object;
        var builder = new FactoryContainerBuilder<string, ITestService>();
        using var container = builder.Build(registrations, scope);

        // Assert
        var resolved = container.TryResolve("shared-key", out var instance);
        await Assert.That(resolved).IsTrue();
        await Assert.That(instance).IsEqualTo(secondService);
    }

    // Aggregate disposal tests (boundary-aware shutdown: attempt every sibling once, aggregate failures)

    [Test]
    public async Task Dispose_WhenFactoryDisposalThrows_AttemptsRemainingSiblingsAnyway()
    {
        // Arrange
        var throwAttempted = false;
        var succeeded = false;
        var throwingFactory = CreateThrowingDisposableMockFactory(new TestService(), () => throwAttempted = true);
        var succeedingFactory = CreateDisposableMockFactory(new TestService(), () => succeeded = true);
        var factories = new Dictionary<string, IKeyedFactory<ITestService>>
        {
            { "throwing", throwingFactory },
            { "succeeding", succeedingFactory }
        };
        var container = new FactoryContainer<string, ITestService>(factories);

        // Act
        try { container.Dispose(); } catch (AggregateException) { }

        // Assert
        await Assert.That(throwAttempted).IsTrue();
        await Assert.That(succeeded).IsTrue();
    }

    [Test]
    public async Task Dispose_WhenFactoryDisposalThrows_ThrowsAggregateExceptionNamingContainer()
    {
        // Arrange
        var throwingFactory = CreateThrowingDisposableMockFactory(new TestService(), () => { });
        var factories = new Dictionary<string, IKeyedFactory<ITestService>>
        {
            { "throwing", throwingFactory }
        };
        var container = new FactoryContainer<string, ITestService>(factories);

        // Act & Assert
        await Assert.That(() => container.Dispose())
            .Throws<AggregateException>()
            .WithMessageMatching("*FactoryContainer*");
    }

    [Test]
    public async Task Dispose_CalledTwiceAfterDisposalFailure_SecondCallIsNoOp()
    {
        // Arrange
        var throwingFactory = CreateThrowingDisposableMockFactory(new TestService(), () => { });
        var factories = new Dictionary<string, IKeyedFactory<ITestService>>
        {
            { "throwing", throwingFactory }
        };
        var container = new FactoryContainer<string, ITestService>(factories);

        // Act
        try { container.Dispose(); } catch (AggregateException) { }

        // Act & Assert
        await Assert.That(() => container.Dispose()).ThrowsNothing();
    }
}
