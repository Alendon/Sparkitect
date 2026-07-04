using Sparkitect.DI;
using Sparkitect.DI.Container;
using Sparkitect.DI.Exceptions;
using Sparkitect.DI.Resolution;

namespace Sparkitect.Tests.DI;

public class CoreContainerBuilderTests
{
    [Test]
    public async Task Build_InstantiationOrder_TieWithOptionalRegisteredDependency_MatchesGolden()
    {
        // Characterization golden (Pitfall 1 — captured before the shared-core swap).
        // Graph (registration/insertion order): RootA, RootB, OptionalDependent, RequiredDependent.
        //   RootA, RootB          -> no dependencies (share in-degree 0: the tie)
        //   OptionalDependent     -> OPTIONAL ctor dep on RootA (registered) => ordering edge RootA -> OptionalDependent
        //   RequiredDependent     -> REQUIRED ctor dep on RootB              => ordering edge RootB -> RequiredDependent
        // The optional-but-registered edge is what proves a present optional constructor dependency
        // still contributes an ordering constraint (the behavior the migration BLOCKER guards).
        InstantiationOrderRecorder.Reset();
        var builder = new CoreContainerBuilder(null);
        builder.Register<RecordingRootAFactory>();
        builder.Register<RecordingRootBFactory>();
        builder.Register<RecordingOptionalDependentFactory>();
        builder.Register<RecordingRequiredDependentFactory>();

        // Act
        var container = builder.Build();

        // Assert — exact instantiation sequence (order-sensitive golden)
        await Assert.That(InstantiationOrderRecorder.Order).IsEquivalentTo(new[]
        {
            typeof(IRecordingRootA),
            typeof(IRecordingRootB),
            typeof(IRecordingOptionalDependent),
            typeof(IRecordingRequiredDependent),
        }, CollectionOrdering.Matching);

        // Invariant that must hold regardless of tiebreak: dependency-before-dependent for BOTH edges,
        // including the optional-but-registered constructor dependency.
        var order = InstantiationOrderRecorder.Order;
        await Assert.That(order.IndexOf(typeof(IRecordingRootA)))
            .IsLessThan(order.IndexOf(typeof(IRecordingOptionalDependent)));
        await Assert.That(order.IndexOf(typeof(IRecordingRootB)))
            .IsLessThan(order.IndexOf(typeof(IRecordingRequiredDependent)));

        await Assert.That(container).IsNotNull();
    }

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

// Instantiation-order characterization support: recording service factories that log the order in
// which the container creates their instances, so a golden can pin container instantiation ordering.
internal static class InstantiationOrderRecorder
{
    public static List<Type> Order { get; } = [];

    public static void Reset() => Order.Clear();

    public static void Record(Type serviceType) => Order.Add(serviceType);
}

public interface IRecordingRootA;

public sealed class RecordingRootA : IRecordingRootA;

public interface IRecordingRootB;

public sealed class RecordingRootB : IRecordingRootB;

public interface IRecordingOptionalDependent;

public sealed class RecordingOptionalDependent(IRecordingRootA? root = null) : IRecordingOptionalDependent
{
    public IRecordingRootA? Root { get; } = root;
}

public interface IRecordingRequiredDependent;

public sealed class RecordingRequiredDependent(IRecordingRootB root) : IRecordingRequiredDependent
{
    public IRecordingRootB Root { get; } = root ?? throw new ArgumentNullException(nameof(root));
}

public sealed class RecordingRootAFactory : IServiceFactory
{
    public Type ServiceType => typeof(IRecordingRootA);
    public Type ImplementationType => typeof(RecordingRootA);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [];
    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(IResolutionScope scope)
    {
        InstantiationOrderRecorder.Record(typeof(IRecordingRootA));
        return new RecordingRootA();
    }

    public void ApplyProperties(object instance, IResolutionScope scope)
    {
    }
}

public sealed class RecordingRootBFactory : IServiceFactory
{
    public Type ServiceType => typeof(IRecordingRootB);
    public Type ImplementationType => typeof(RecordingRootB);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [];
    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(IResolutionScope scope)
    {
        InstantiationOrderRecorder.Record(typeof(IRecordingRootB));
        return new RecordingRootB();
    }

    public void ApplyProperties(object instance, IResolutionScope scope)
    {
    }
}

public sealed class RecordingOptionalDependentFactory : IServiceFactory
{
    public Type ServiceType => typeof(IRecordingOptionalDependent);
    public Type ImplementationType => typeof(RecordingOptionalDependent);

    // Optional constructor dependency on RootA — registered in the characterization graph, so the
    // current code contributes an ordering edge (RootA before this dependent).
    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [(typeof(IRecordingRootA), true)];
    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(IResolutionScope scope)
    {
        InstantiationOrderRecorder.Record(typeof(IRecordingOptionalDependent));
        scope.TryResolve<IRecordingRootA>(GetType(), out var root);
        return new RecordingOptionalDependent(root);
    }

    public void ApplyProperties(object instance, IResolutionScope scope)
    {
    }
}

public sealed class RecordingRequiredDependentFactory : IServiceFactory
{
    public Type ServiceType => typeof(IRecordingRequiredDependent);
    public Type ImplementationType => typeof(RecordingRequiredDependent);

    public (Type Type, bool IsOptional)[] GetConstructorDependencies() => [(typeof(IRecordingRootB), false)];
    public (Type Type, bool IsOptional)[] GetPropertyDependencies() => [];

    public object CreateInstance(IResolutionScope scope)
    {
        InstantiationOrderRecorder.Record(typeof(IRecordingRequiredDependent));
        if (!scope.TryResolve<IRecordingRootB>(GetType(), out var root))
            throw new DependencyResolutionException("Failed to resolve dependency IRecordingRootB");

        return new RecordingRequiredDependent(root);
    }

    public void ApplyProperties(object instance, IResolutionScope scope)
    {
    }
}