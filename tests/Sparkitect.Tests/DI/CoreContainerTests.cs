using System.Diagnostics.CodeAnalysis;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.Exceptions;

namespace Sparkitect.Tests.DI;

public class CoreContainerTests
{
    [Test]
    public async Task Resolve_RegisteredService_ReturnsService()
    {
        // Arrange
        var instances = new Dictionary<Type, object>
        {
            { typeof(ITestService), new TestService() }
        };
        
        var container = new CoreContainer(instances, null);
        
        // Act
        var service = container.Resolve<ITestService>();
        
        // Assert
        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<TestService>();
    }
    
    [Test]
    public async Task Resolve_UnregisteredService_ThrowsDependencyResolutionException()
    {
        // Arrange
        var instances = new Dictionary<Type, object>();
        var container = new CoreContainer(instances, null);
        
        // Act & Assert
        await Assert.That(() => container.Resolve<ITestService>()).Throws<DependencyResolutionException>();
    }
    
    [Test]
    public async Task TryResolve_RegisteredService_ReturnsTrue()
    {
        // Arrange
        var instances = new Dictionary<Type, object>
        {
            { typeof(ITestService), new TestService() }
        };
        
        var container = new CoreContainer(instances, null);
        
        // Act
        var result = container.TryResolve<ITestService>(out var service);
        
        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<TestService>();
    }
    
    [Test]
    public async Task TryResolve_UnregisteredService_ReturnsFalse()
    {
        // Arrange
        var instances = new Dictionary<Type, object>();
        var container = new CoreContainer(instances, null);
        
        // Act
        var result = container.TryResolve<ITestService>(out var service);
        
        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(service).IsNull();
    }
    
    [Test]
    public async Task Dispose_DisposesRegisteredDisposableServices()
    {
        // Arrange
        var disposableService = new DisposableTestService();
        var instances = new Dictionary<Type, object>
        {
            { typeof(IDisposableTestService), disposableService }
        };
        
        var container = new CoreContainer(instances, null);
        
        // Act
        container.Dispose();
        
        // Assert
        await Assert.That(disposableService.IsDisposed).IsTrue();
    }
    
    [Test]
    public async Task Resolve_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var instances = new Dictionary<Type, object>
        {
            { typeof(ITestService), new TestService() }
        };
        
        var container = new CoreContainer(instances, null);
        container.Dispose();
        
        // Act & Assert
        await Assert.That(() => container.Resolve<ITestService>())
            .Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task GetRegisteredInstances_ReturnsAllInstances()
    {
        // Arrange
        var testService = new TestService();
        var dependencyService = new DependencyService();
        var instances = new Dictionary<Type, object>
        {
            { typeof(ITestService), testService },
            { typeof(IDependencyService), dependencyService }
        };

        var container = new CoreContainer(instances, null);

        // Act
        var registeredInstances = container.GetCurrentRegisteredInstances();

        // Assert
        await Assert.That(registeredInstances).IsNotNull();
        await Assert.That(registeredInstances.Count).IsEqualTo(2);
        await Assert.That(registeredInstances.ContainsKey(typeof(ITestService))).IsTrue();
        await Assert.That(registeredInstances.ContainsKey(typeof(IDependencyService))).IsTrue();
        await Assert.That(registeredInstances[typeof(ITestService)]).IsEqualTo(testService);
        await Assert.That(registeredInstances[typeof(IDependencyService)]).IsEqualTo(dependencyService);
    }

    // Parent resolution tests (hierarchical container behavior)

    [Test]
    public async Task TryResolve_WithParentContainer_ResolvesFromParent()
    {
        // Arrange - parent has service, child doesn't
        var parentService = new TestService();
        var parentInstances = new Dictionary<Type, object>
        {
            { typeof(ITestService), parentService }
        };
        var parentContainer = new CoreContainer(parentInstances, null);

        var childInstances = new Dictionary<Type, object>(); // Empty child
        var childContainer = new CoreContainer(childInstances, parentContainer);

        // Act
        var result = childContainer.TryResolve<ITestService>(out var service);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsEqualTo(parentService);
    }

    [Test]
    public async Task TryResolve_ParentAndChild_ParentTakesPriority()
    {
        // Arrange - both parent and child have the same service type
        var parentService = new TestService();
        var childService = new OverrideTestService();

        var parentInstances = new Dictionary<Type, object>
        {
            { typeof(ITestService), parentService }
        };
        var parentContainer = new CoreContainer(parentInstances, null);

        var childInstances = new Dictionary<Type, object>
        {
            { typeof(ITestService), childService }
        };
        var childContainer = new CoreContainer(childInstances, parentContainer);

        // Act
        var result = childContainer.TryResolve<ITestService>(out var service);

        // Assert - parent takes priority (short-circuit OR in source)
        await Assert.That(result).IsTrue();
        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsEqualTo(parentService);
    }

    [Test]
    public async Task TryResolve_OnlyInChild_ResolvesFromChild()
    {
        // Arrange - parent doesn't have service, child does
        var parentInstances = new Dictionary<Type, object>(); // Empty parent
        var parentContainer = new CoreContainer(parentInstances, null);

        var childService = new TestService();
        var childInstances = new Dictionary<Type, object>
        {
            { typeof(ITestService), childService }
        };
        var childContainer = new CoreContainer(childInstances, parentContainer);

        // Act
        var result = childContainer.TryResolve<ITestService>(out var service);

        // Assert
        await Assert.That(result).IsTrue();
        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsEqualTo(childService);
    }

    [Test]
    public async Task TryResolve_NeitherHas_ReturnsFalse()
    {
        // Arrange - neither parent nor child has the service
        var parentInstances = new Dictionary<Type, object>();
        var parentContainer = new CoreContainer(parentInstances, null);

        var childInstances = new Dictionary<Type, object>();
        var childContainer = new CoreContainer(childInstances, parentContainer);

        // Act
        var result = childContainer.TryResolve<ITestService>(out var service);

        // Assert
        await Assert.That(result).IsFalse();
        await Assert.That(service).IsNull();
    }

    [Test]
    public async Task Resolve_WithParentContainer_ResolvesFromParent()
    {
        // Arrange - parent has service, child doesn't
        var parentService = new TestService();
        var parentInstances = new Dictionary<Type, object>
        {
            { typeof(ITestService), parentService }
        };
        var parentContainer = new CoreContainer(parentInstances, null);

        var childInstances = new Dictionary<Type, object>(); // Empty child
        var childContainer = new CoreContainer(childInstances, parentContainer);

        // Act
        var service = childContainer.Resolve<ITestService>();

        // Assert
        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsEqualTo(parentService);
    }
}