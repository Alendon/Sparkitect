using Sparkitect.DI;
using Sparkitect.DI.Exceptions;

namespace Sparkitect.Tests.DI;

// Service Factory implementations
public class TestServiceFactory : IServiceFactory
{
    public Type ServiceType => typeof(ITestService);
    public Type ImplementationType => typeof(TestService);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [];
    public (Type Type, string PropertyName, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(ICoreContainerBuilder container) => new TestService();

    public void ApplyProperties(object instance, ICoreContainerBuilder container)
    {
    }
}

public class OverrideTestServiceFactory : IServiceFactory
{
    public Type ServiceType => typeof(ITestService);
    public Type ImplementationType => typeof(OverrideTestService);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [];
    public (Type Type, string PropertyName, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(ICoreContainerBuilder container) => new OverrideTestService();

    public void ApplyProperties(object instance, ICoreContainerBuilder container)
    {
    }
}

public class DuplicateTestServiceFactory : IServiceFactory
{
    public Type ServiceType => typeof(ITestService);
    public Type ImplementationType => typeof(TestService);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [];
    public (Type Type, string PropertyName, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(ICoreContainerBuilder container) => new TestService();

    public void ApplyProperties(object instance, ICoreContainerBuilder container)
    {
    }
}

public class DependencyServiceFactory : IServiceFactory
{
    public Type ServiceType => typeof(IDependencyService);
    public Type ImplementationType => typeof(DependencyService);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [];
    public (Type Type, string PropertyName, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(ICoreContainerBuilder container) => new DependencyService();

    public void ApplyProperties(object instance, ICoreContainerBuilder container)
    {
    }
}

public class DependentServiceFactory : IServiceFactory
{
    public Type ServiceType => typeof(IDependentService);
    public Type ImplementationType => typeof(DependentService);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [(typeof(IDependencyService), false)];
    public (Type Type, string PropertyName, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(ICoreContainerBuilder container)
    {
        if (!container.TryResolveInternal<IDependencyService>(out var dependency))
            throw new DependencyResolutionException("Failed to resolve dependency IDependencyService");

        return new DependentService(dependency);
    }

    public void ApplyProperties(object instance, ICoreContainerBuilder container)
    {
    }
}

public class CircularService1Factory : IServiceFactory
{
    public Type ServiceType => typeof(ICircularService1);
    public Type ImplementationType => typeof(CircularService1);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [(typeof(ICircularService2), false)];
    public (Type Type, string PropertyName, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(ICoreContainerBuilder container)
    {
        if (!container.TryResolveInternal<ICircularService2>(out var dependency))
            throw new DependencyResolutionException("Failed to resolve dependency ICircularService2");

        return new CircularService1(dependency);
    }

    public void ApplyProperties(object instance, ICoreContainerBuilder container)
    {
    }
}

public class CircularService2Factory : IServiceFactory
{
    public Type ServiceType => typeof(ICircularService2);
    public Type ImplementationType => typeof(CircularService2);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [(typeof(ICircularService1), false)];
    public (Type Type, string PropertyName, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(ICoreContainerBuilder container)
    {
        if (!container.TryResolveInternal<ICircularService1>(out var dependency))
            throw new DependencyResolutionException("Failed to resolve dependency ICircularService1");

        return new CircularService2(dependency);
    }

    public void ApplyProperties(object instance, ICoreContainerBuilder container)
    {
    }
}

public class ServiceWithOptionalPropertyDependencyFactory : IServiceFactory
{
    public Type ServiceType => typeof(IServiceWithOptionalPropertyDependency);
    public Type ImplementationType => typeof(ServiceWithOptionalPropertyDependency);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [];

    public (Type Type, string PropertyName, bool IsOptional)[] GetPropertyDependencies() =>
    [
        (typeof(IDependencyService), "OptionalDependency", true)
    ];

    public object CreateInstance(ICoreContainerBuilder container) => new ServiceWithOptionalPropertyDependency();

    public void ApplyProperties(object instance, ICoreContainerBuilder container)
    {
        var service = (ServiceWithOptionalPropertyDependency)instance;
        if (container.TryResolveInternal<IDependencyService>(out var dependency))
            service.OptionalDependency = dependency;
    }
}

public class ServiceWithOptionalConstructorDependencyFactory : IServiceFactory
{
    public Type ServiceType => typeof(IServiceWithOptionalConstructorDependency);
    public Type ImplementationType => typeof(ServiceWithOptionalConstructorDependency);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [(typeof(IDependencyService), true)];
    public (Type Type, string PropertyName, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(ICoreContainerBuilder container)
    {
        if (container.TryResolveInternal<IDependencyService>(out var dependency))
            return new ServiceWithOptionalConstructorDependency(dependency);

        return new ServiceWithOptionalConstructorDependency();
    }

    public void ApplyProperties(object instance, ICoreContainerBuilder container)
    {
    }
}