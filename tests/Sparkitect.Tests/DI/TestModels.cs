namespace Sparkitect.Tests.DI;

// Test interfaces and implementations
public interface ITestService;
public class TestService : ITestService;
public class OverrideTestService : ITestService;

public interface IDisposableTestService : IDisposable;
public class DisposableTestService : IDisposableTestService
{
    public bool IsDisposed { get; private set; }
    
    public void Dispose()
    {
        IsDisposed = true;
    }
}

public interface IDependencyService;
public class DependencyService : IDependencyService;

public interface IDependentService
{
    IDependencyService Dependency { get; }
}

public class DependentService : IDependentService
{
    public IDependencyService Dependency { get; }
    
    public DependentService(IDependencyService dependency)
    {
        Dependency = dependency ?? throw new ArgumentNullException(nameof(dependency));
    }
}

public interface IServiceWithProperties
{
    IDependencyService? Dependency { get; set; }
}

public class ServiceWithProperties : IServiceWithProperties
{
    public IDependencyService? Dependency { get; set; }
}

// Models for optional dependencies
public interface IServiceWithOptionalPropertyDependency
{
    IDependencyService? OptionalDependency { get; set; }
}

public class ServiceWithOptionalPropertyDependency : IServiceWithOptionalPropertyDependency
{
    public IDependencyService? OptionalDependency { get; set; }
}

public interface IServiceWithOptionalConstructorDependency
{
    IDependencyService? OptionalDependency { get; }
}

public class ServiceWithOptionalConstructorDependency(IDependencyService? optionalDependency = null)
    : IServiceWithOptionalConstructorDependency
{
    public IDependencyService? OptionalDependency { get; } = optionalDependency;
}

// Circular dependency classes
public interface ICircularService1
{
    ICircularService2 Service2 { get; }
}

public interface ICircularService2
{
    ICircularService1 Service1 { get; }
}

public class CircularService1 : ICircularService1
{
    public ICircularService2 Service2 { get; }
    
    public CircularService1(ICircularService2 service2)
    {
        Service2 = service2 ?? throw new ArgumentNullException(nameof(service2));
    }
}

public class CircularService2 : ICircularService2
{
    public ICircularService1 Service1 { get; }

    public CircularService2(ICircularService1 service1)
    {
        Service1 = service1 ?? throw new ArgumentNullException(nameof(service1));
    }
}

// Entrypoint test models
public interface IEntrypointService;
public class EntrypointService : IEntrypointService;
public class AnotherEntrypointService : IEntrypointService;

public class DisposableEntrypointService : IEntrypointService, IDisposable
{
    public bool IsDisposed { get; private set; }

    public void Dispose()
    {
        IsDisposed = true;
    }
}