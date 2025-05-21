using System.Diagnostics.CodeAnalysis;
using Sparkitect.DI;
using Sparkitect.DI.Exceptions;
using TUnit.Assertions.AssertConditions.Throws;

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
        
        var container = new CoreContainer(instances);
        
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
        var container = new CoreContainer(instances);
        
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
        
        var container = new CoreContainer(instances);
        
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
        var container = new CoreContainer(instances);
        
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
        
        var container = new CoreContainer(instances);
        
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
        
        var container = new CoreContainer(instances);
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
        
        var container = new CoreContainer(instances);
        
        // Act
        var registeredInstances = container.GetRegisteredInstances();
        
        // Assert
        await Assert.That(registeredInstances).IsNotNull();
        await Assert.That(registeredInstances.Count).IsEqualTo(2);
        await Assert.That(registeredInstances.ContainsKey(typeof(ITestService))).IsTrue();
        await Assert.That(registeredInstances.ContainsKey(typeof(IDependencyService))).IsTrue();
        await Assert.That(registeredInstances[typeof(ITestService)]).IsEqualTo(testService);
        await Assert.That(registeredInstances[typeof(IDependencyService)]).IsEqualTo(dependencyService);
    }
}