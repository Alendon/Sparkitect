using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.Exceptions;

namespace Sparkitect.Tests.DI;

/// <summary>
/// Tests to verify the circular dependency detection in the DI system.
/// </summary>
public class CircularDependencyTests
{
    [Test]
    public async Task Build_WithCircularDependency_ThrowsCircularDependencyException()
    {
        // Arrange
        var builder = new CoreContainerBuilder(null);
        builder.Register<CircularService1Factory>();
        builder.Register<CircularService2Factory>();
        
        // Act & Assert
        await Assert.That(() => builder.Build()).Throws<CircularDependencyException>();
    }
    
    [Test]
    public async Task ValidateDependencyGraph_CircularThroughProperties_DoesNotThrow()
    {
        // Arrange - Create factories that form a cycle through property dependencies
        var builder = new CoreContainerBuilder(null);
        
        // Register factories with circular property dependencies
        builder.Register<PropertyCircularService1Factory>();
        builder.Register<PropertyCircularService2Factory>();
        
        // Act & Assert
        await Assert.That(() => builder.Build()).ThrowsNothing();
    }
    
    [Test]
    public async Task ValidateDependencyGraph_CircularMixed_DoesNotThrow()
    {
        // Arrange - Create factories that form a cycle through property dependencies
        var builder = new CoreContainerBuilder(null);
        
        // Register factories with circular property dependencies
        builder.Register<CircularService1Factory>();
        builder.Register<PropertyCircularService2Factory>();
        
        // Act & Assert
        await Assert.That(() => builder.Build()).ThrowsNothing();
    }
}

/// <summary>
/// Service factories with circular dependencies through properties
/// </summary>
public class PropertyCircularService1Factory : IServiceFactory
{
    public Type ServiceType => typeof(ICircularService1);
    public Type ImplementationType => typeof(PropertyCircularService1);
    
    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [];
    
    public (Type Type,  bool IsOptional)[] GetPropertyDependencies() => 
    [
        (typeof(ICircularService2),false)
    ];
    
    public object CreateInstance(ICoreContainerBuilder container) => new PropertyCircularService1();
    
    public void ApplyProperties(object instance, ICoreContainerBuilder container)
    {
        var service = (PropertyCircularService1)instance;
        if (container.TryResolveInternal<ICircularService2>(out var dependency))
            service.Service2 = dependency;
    }
}

public class PropertyCircularService2Factory : IServiceFactory
{
    public Type ServiceType => typeof(ICircularService2);
    public Type ImplementationType => typeof(PropertyCircularService2);
    
    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [];
    
    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => 
    [
        (typeof(ICircularService1), false)
    ];
    
    public object CreateInstance(ICoreContainerBuilder container) => new PropertyCircularService2();
    
    public void ApplyProperties(object instance, ICoreContainerBuilder container)
    {
        var service = (PropertyCircularService2)instance;
        if (container.TryResolveInternal<ICircularService1>(out var dependency))
            service.Service1 = dependency;
    }
}


public class PropertyCircularService1 : ICircularService1
{
    public ICircularService2 Service2 { get; set; } = null!;
}

public class PropertyCircularService2 : ICircularService2
{
    public ICircularService1 Service1 { get; set; } = null!;
}