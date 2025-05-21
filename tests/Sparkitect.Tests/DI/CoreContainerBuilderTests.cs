using Sparkitect.DI;
using Sparkitect.DI.Exceptions;
using TUnit.Assertions.AssertConditions.Throws;

namespace Sparkitect.Tests.DI;

public class CoreContainerBuilderTests
{
    [Test]
    public async Task Register_ValidServiceFactory_RegistersService()
    {
        // Arrange
        var builder = new CoreContainerBuilder();
        var factory = new TestServiceFactory();
        
        // Act
        builder.Register(factory);
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
        var builder = new CoreContainerBuilder();
        var factory1 = new TestServiceFactory();
        var factory2 = new DuplicateTestServiceFactory();
        
        // Act & Assert
        builder.Register(factory1);
        await Assert.That(() => builder.Register(factory2)).Throws<InvalidOperationException>();
    }
    
    [Test]
    public async Task Build_RegisteredServices_CreatesCoreContainer()
    {
        // Arrange
        var builder = new CoreContainerBuilder();
        builder.Register(new TestServiceFactory());
        builder.Register(new DependencyServiceFactory());
        
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
        var builder = new CoreContainerBuilder();
        builder.Register(new DependencyServiceFactory());
        builder.Register(new DependentServiceFactory());
        
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
        var builder = new CoreContainerBuilder();
        builder.Register(new DependentServiceFactory());
        
        // Act & Assert
        await Assert.That(() => builder.Build()).Throws<DependencyResolutionException>();
    }
    
    [Test]
    public async Task Override_RegisteredService_OverridesImplementation()
    {
        // Arrange
        var builder = new CoreContainerBuilder();
        builder.Register(new TestServiceFactory());
        
        // Act
        builder.Override(new OverrideTestServiceFactory());
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
        var builder = new CoreContainerBuilder();
        var factory = new OverrideTestServiceFactory();
        
        // Act & Assert
        await Assert.That(() => builder.Override(factory)).Throws<InvalidOperationException>();
    }
}