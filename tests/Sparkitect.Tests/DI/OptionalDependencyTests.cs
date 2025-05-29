using TUnit.Assertions.AssertConditions.Throws;
using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.Exceptions;

namespace Sparkitect.Tests.DI;

/// <summary>
/// Tests for verifying optional dependency handling in the DI system.
/// </summary>
public class OptionalDependencyTests
{
    [Test]
    public async Task OptionalPropertyDependency_WhenMissing_DoesNotThrow()
    {
        // Arrange
        var builder = new CoreContainerBuilder(null);
        builder.Register<ServiceWithOptionalPropertyDependencyFactory>();
        // Not registering DependencyService
        
        // Act & Assert - should not throw since the dependency is optional
        await Assert.That(() => builder.Build()).ThrowsNothing();
    }
    
    [Test]
    public async Task OptionalPropertyDependency_WhenAvailable_IsResolved()
    {
        // Arrange
        var builder = new CoreContainerBuilder(null);
        builder.Register<ServiceWithOptionalPropertyDependencyFactory>();
        builder.Register<DependencyServiceFactory>();
        
        // Act
        var container = builder.Build();
        
        // Assert
        var service = container.Resolve<IServiceWithOptionalPropertyDependency>();
        await Assert.That(service).IsNotNull();
        await Assert.That(service.OptionalDependency).IsNotNull();
        await Assert.That(service.OptionalDependency).IsTypeOf<DependencyService>();
    }
    
    [Test]
    public async Task OptionalConstructorDependency_WhenMissing_DoesNotThrow()
    {
        // Arrange
        var builder = new CoreContainerBuilder(null);
        builder.Register<ServiceWithOptionalConstructorDependencyFactory>();
        // Not registering DependencyService
        
        // Act & Assert - should not throw since the dependency is optional
        await Assert.That(() => builder.Build()).ThrowsNothing();
    }
    
    [Test]
    public async Task OptionalConstructorDependency_WhenMissing_CreatesServiceWithoutDependency()
    {
        // Arrange
        var builder = new CoreContainerBuilder(null);
        builder.Register<ServiceWithOptionalConstructorDependencyFactory>();
        // Not registering DependencyService
        
        // Act
        var container = builder.Build();
        
        // Assert
        var service = container.Resolve<IServiceWithOptionalConstructorDependency>();
        await Assert.That(service).IsNotNull();
        await Assert.That(service.OptionalDependency).IsNull();
    }
    
    [Test]
    public async Task OptionalConstructorDependency_WhenAvailable_IsResolved()
    {
        // Arrange
        var builder = new CoreContainerBuilder(null);
        builder.Register<ServiceWithOptionalConstructorDependencyFactory>();
        builder.Register<DependencyServiceFactory>();
        
        // Act
        var container = builder.Build();
        
        // Assert
        var service = container.Resolve<IServiceWithOptionalConstructorDependency>();
        await Assert.That(service).IsNotNull();
        await Assert.That(service.OptionalDependency).IsNotNull();
        await Assert.That(service.OptionalDependency).IsTypeOf<DependencyService>();
    }
}