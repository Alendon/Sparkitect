using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.Exceptions;

namespace Sparkitect.Tests.DI;

public class CoreContainerBuilderTests
{
    [Test]
    public async Task Register_ValidServiceFactory_RegistersService()
    {
        // Arrange
        var builder = new CoreContainerBuilder(null);
        
        // Act
        builder.Register<TestServiceFactory>();
        var container = builder.Build();
        
        // Assert
        await Assert.That(container).IsNotNull();
        var service = container.Resolve<ITestService>();
        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<TestService>();
    }
    
    [Test]
    public async Task Register_DuplicateServiceFactory_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new CoreContainerBuilder(null);
        var factory1 = new TestServiceFactory();
        var factory2 = new DuplicateTestServiceFactory();
        
        // Act & Assert
        builder.Register<TestServiceFactory>();
        await Assert.That(() => builder.Register<DuplicateTestServiceFactory>()).Throws<InvalidOperationException>();
    }
    
    [Test]
    public async Task Build_RegisteredServices_CreatesCoreContainer()
    {
        // Arrange
        var builder = new CoreContainerBuilder(null);
        builder.Register<TestServiceFactory>();
        builder.Register<DependencyServiceFactory>();
        
        // Act
        var container = builder.Build();
        
        // Assert
        await Assert.That(container).IsNotNull();
        await Assert.That(container).IsTypeOf<CoreContainer>();
        
        var testService = container.Resolve<ITestService>();
        var dependencyService = container.Resolve<IDependencyService>();
        
        await Assert.That(testService).IsNotNull();
        await Assert.That(dependencyService).IsNotNull();
    }
    
    [Test]
    public async Task Build_WithDependentService_ResolvesInCorrectOrder()
    {
        // Arrange
        var builder = new CoreContainerBuilder(null);
        builder.Register<DependencyServiceFactory>();
        builder.Register<DependentServiceFactory>();
        
        // Act
        var container = builder.Build();
        
        // Assert
        await Assert.That(container).IsNotNull();
        
        var dependentService = container.Resolve<IDependentService>();
        await Assert.That(dependentService).IsNotNull();
        await Assert.That(dependentService.Dependency).IsNotNull();
        await Assert.That(dependentService.Dependency).IsTypeOf<DependencyService>();
    }
    
    [Test]
    public async Task Build_MissingDependency_ThrowsDependencyResolutionException()
    {
        // Arrange
        var builder = new CoreContainerBuilder(null);
        builder.Register<DependentServiceFactory>();
        
        // Act & Assert
        await Assert.That(() => builder.Build()).Throws<DependencyResolutionException>();
    }
    
    [Test]
    public async Task Override_RegisteredService_OverridesImplementation()
    {
        // Arrange
        var builder = new CoreContainerBuilder(null);
        builder.Register<TestServiceFactory>();
        
        // Act
        builder.Override<OverrideTestServiceFactory>();
        var container = builder.Build();
        
        // Assert
        await Assert.That(container).IsNotNull();
        var service = container.Resolve<ITestService>();
        await Assert.That(service).IsNotNull();
        await Assert.That(service).IsTypeOf<OverrideTestService>();
    }
    
    [Test]
    public async Task Override_UnregisteredService_ThrowsInvalidOperationException()
    {
        // Arrange
        var builder = new CoreContainerBuilder(null);
        
        // Act & Assert
        await Assert.That(() => builder.Override<OverrideTestServiceFactory>()).Throws<InvalidOperationException>();
    }
}