using Sparkitect.DI;
using Sparkitect.DI.Exceptions;
using Sparkitect.DI.Resolution;

namespace Sparkitect.Tests.DI;

// Service Factory implementations
public class TestServiceFactory : IServiceFactory
{
    public Type ServiceType => typeof(ITestService);
    public Type ImplementationType => typeof(TestService);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [];
    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(IResolutionScope scope) => new TestService();

    public void ApplyProperties(object instance, IResolutionScope scope)
    {
    }
}

public class OverrideTestServiceFactory : IServiceFactory
{
    public Type ServiceType => typeof(ITestService);
    public Type ImplementationType => typeof(OverrideTestService);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [];
    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(IResolutionScope scope) => new OverrideTestService();

    public void ApplyProperties(object instance, IResolutionScope scope)
    {
    }
}

public class DuplicateTestServiceFactory : IServiceFactory
{
    public Type ServiceType => typeof(ITestService);
    public Type ImplementationType => typeof(TestService);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [];
    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(IResolutionScope scope) => new TestService();

    public void ApplyProperties(object instance, IResolutionScope scope)
    {
    }
}

public class DependencyServiceFactory : IServiceFactory
{
    public Type ServiceType => typeof(IDependencyService);
    public Type ImplementationType => typeof(DependencyService);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [];
    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(IResolutionScope scope) => new DependencyService();

    public void ApplyProperties(object instance, IResolutionScope scope)
    {
    }
}

public class DependentServiceFactory : IServiceFactory
{
    public Type ServiceType => typeof(IDependentService);
    public Type ImplementationType => typeof(DependentService);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [(typeof(IDependencyService), false)];
    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(IResolutionScope scope)
    {
        if (!scope.TryResolve<IDependencyService>(GetType(), out var dependency))
            throw new DependencyResolutionException("Failed to resolve dependency IDependencyService");

        return new DependentService(dependency);
    }

    public void ApplyProperties(object instance, IResolutionScope scope)
    {
    }
}

public class CircularService1Factory : IServiceFactory
{
    public Type ServiceType => typeof(ICircularService1);
    public Type ImplementationType => typeof(CircularService1);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [(typeof(ICircularService2), false)];
    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(IResolutionScope scope)
    {
        if (!scope.TryResolve<ICircularService2>(GetType(), out var dependency))
            throw new DependencyResolutionException("Failed to resolve dependency ICircularService2");

        return new CircularService1(dependency);
    }

    public void ApplyProperties(object instance, IResolutionScope scope)
    {
    }
}

public class CircularService2Factory : IServiceFactory
{
    public Type ServiceType => typeof(ICircularService2);
    public Type ImplementationType => typeof(CircularService2);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [(typeof(ICircularService1), false)];
    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(IResolutionScope scope)
    {
        if (!scope.TryResolve<ICircularService1>(GetType(), out var dependency))
            throw new DependencyResolutionException("Failed to resolve dependency ICircularService1");

        return new CircularService2(dependency);
    }

    public void ApplyProperties(object instance, IResolutionScope scope)
    {
    }
}

public class ServiceWithOptionalPropertyDependencyFactory : IServiceFactory
{
    public Type ServiceType => typeof(IServiceWithOptionalPropertyDependency);
    public Type ImplementationType => typeof(ServiceWithOptionalPropertyDependency);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [];

    public (Type Type, bool IsOptional)[] GetPropertyDependencies() =>
    [
        (typeof(IDependencyService), true)
    ];

    public object CreateInstance(IResolutionScope scope) => new ServiceWithOptionalPropertyDependency();

    public void ApplyProperties(object instance, IResolutionScope scope)
    {
        var service = (ServiceWithOptionalPropertyDependency)instance;
        if (scope.TryResolve<IDependencyService>(GetType(), out var dependency))
            service.OptionalDependency = dependency;
    }
}

public class ServiceWithOptionalConstructorDependencyFactory : IServiceFactory
{
    public Type ServiceType => typeof(IServiceWithOptionalConstructorDependency);
    public Type ImplementationType => typeof(ServiceWithOptionalConstructorDependency);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [(typeof(IDependencyService), true)];
    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(IResolutionScope scope)
    {
        if (scope.TryResolve<IDependencyService>(GetType(), out var dependency))
            return new ServiceWithOptionalConstructorDependency(dependency);

        return new ServiceWithOptionalConstructorDependency();
    }

    public void ApplyProperties(object instance, IResolutionScope scope)
    {
    }
}
