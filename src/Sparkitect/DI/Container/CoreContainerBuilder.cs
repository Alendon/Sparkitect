using System.Diagnostics.CodeAnalysis;
using Sparkitect.DI.Exceptions;
using Sparkitect.DI.Resolution;
using Sparkitect.Utils.DU;
using Sparkitect.Utils.Ordering;

namespace Sparkitect.DI.Container;

internal class CoreContainerBuilder : ICoreContainerBuilder
{
    private readonly Dictionary<Type, IServiceFactory> _registrations = [];
    private readonly Dictionary<Type, object> _instances = [];
    private readonly ICoreContainer? _parentContainer;

    public CoreContainerBuilder(ICoreContainer? parentContainer)
    {
        _parentContainer = parentContainer;
    }

    public ICoreContainerBuilder Register<TServiceFactory>() where TServiceFactory : IServiceFactory, new()
    {
        var serviceFactory = new TServiceFactory();
        var serviceType = serviceFactory.ServiceType;

        if (_parentContainer?.TryResolve(serviceType, out _) is true)
            throw new InvalidOperationException(
                $"Service {serviceType.Name} is already registered in the parent container");


        if (!_registrations.TryAdd(serviceType, serviceFactory))
            throw new InvalidOperationException($"Service {serviceType.Name} is already registered");

        return this;
    }

    public ICoreContainerBuilder Register(Type factoryType)
    {
        if (factoryType is null) throw new ArgumentNullException(nameof(factoryType));
        if (!typeof(IServiceFactory).IsAssignableFrom(factoryType))
            throw new ArgumentException(
                $"{factoryType.FullName} does not implement IServiceFactory.",
                nameof(factoryType));

        var instance = Activator.CreateInstance(factoryType)
            ?? throw new InvalidOperationException(
                $"Failed to instantiate service factory {factoryType.FullName} — requires a parameterless constructor.");
        var serviceFactory = (IServiceFactory)instance;
        var serviceType = serviceFactory.ServiceType;

        if (_parentContainer?.TryResolve(serviceType, out _) is true)
            throw new InvalidOperationException(
                $"Service {serviceType.Name} is already registered in the parent container");

        if (!_registrations.TryAdd(serviceType, serviceFactory))
            throw new InvalidOperationException($"Service {serviceType.Name} is already registered");

        return this;
    }

    public ICoreContainerBuilder Override<TServiceFactory>() where TServiceFactory : IServiceFactory, new()
    {
        var serviceFactory = new TServiceFactory();
        var serviceType = serviceFactory.ServiceType;

        if (!_registrations.ContainsKey(serviceType))
            throw new InvalidOperationException($"Cannot override {serviceType.Name} as it is not registered");

        _registrations[serviceType] = serviceFactory;
        return this;
    }

    public ICoreContainer Build()
    {
        // The constructor-dependency graph is built once. Its shared-core sort IS the cycle check,
        // and the resulting order drives instantiation. Property-dependency existence is a separate pass.
        var sortedTypes = ResolveInstantiationOrder();
        ValidatePropertyDependencies();
        InstantiateServices(sortedTypes);

        return new CoreContainer(_instances, _parentContainer);
    }

    public bool TryResolveInternal<T>([NotNullWhen(true)] out T? instance) where T : class
    {
        instance = null;

        if (_parentContainer?.TryResolve(out instance) is true)
        {
            return true;
        }

        if (_instances.TryGetValue(typeof(T), out var obj))
        {
            instance = (T)obj;
            return true;
        }

        return false;
    }

    public bool TryResolveInternal(Type serviceType, out object? instance)
    {
        instance = null;

        if (_parentContainer?.TryResolve(serviceType, out instance) is true)
        {
            return true;
        }

        if (_instances.TryGetValue(serviceType, out instance))
        {
            return true;
        }

        return false;
    }

    private IReadOnlyList<Type> ResolveInstantiationOrder()
    {
        var graph = new OrderingGraphBuilder<Type>();

        // Feed nodes in _registrations.Keys order (the current de-facto determinism) so the
        // insertion-order tiebreak reproduces registration order among independent services.
        foreach (var serviceType in _registrations.Keys)
        {
            graph.AddNode(serviceType);
        }

        // Add an ordering edge (dependency before dependent) for EVERY registered constructor
        // dependency — required OR optional. The IsOptional flag gates only whether a MISSING
        // dependency is fatal; a PRESENT one always contributes an ordering constraint. Every edge
        // targets a registered node, so the core-edge optional flag is moot (uniform false).
        foreach (var serviceFactory in _registrations.Values)
        {
            var serviceType = serviceFactory.ServiceType;

            foreach (var (dependencyType, isOptional) in serviceFactory.GetConstructorDependencies())
            {
                if (_parentContainer?.TryResolve(dependencyType, out _) is true) continue;

                if (!_registrations.ContainsKey(dependencyType))
                {
                    if (isOptional) continue;

                    throw DependencyResolutionException.CreateForConstructor(
                        serviceType, dependencyType, "unknown");
                }

                graph.AddEdge(dependencyType, serviceType, optional: false);
            }
        }

        // The sort IS the cycle check: a Cycle error becomes CircularDependencyException. No
        // MissingRequiredDependency arm is reachable here because every edge targets a registered node.
        var sort = graph.Sort(OrderingTiebreak<Type>.InsertionOrder);
        if (sort is Result<IReadOnlyList<Type>, OrderingError<Type>>.Ok ok)
        {
            return ok.Value;
        }

        var error = ((Result<IReadOnlyList<Type>, OrderingError<Type>>.Error)sort).Value;
        var participants = error is OrderingError<Type>.Cycle cycle
            ? string.Join(" -> ", cycle.Participants.Select(type => type.Name))
            : string.Empty;

        throw new CircularDependencyException(
            participants.Length == 0
                ? "Circular dependency detected in the container"
                : $"Circular dependency detected in the container: {participants}");
    }

    private void ValidatePropertyDependencies()
    {
        // Property dependencies are a separate existence pass — property cycles are permitted (the
        // two-pass instantiate resolves them), only a missing required property dependency throws.
        foreach (var serviceFactory in _registrations.Values)
        {
            var serviceType = serviceFactory.ServiceType;

            foreach (var (dependencyType, isOptional) in serviceFactory.GetPropertyDependencies())
            {
                if (isOptional)
                    continue;

                if (_parentContainer?.TryResolve(dependencyType, out _) is true) continue;

                if (!_registrations.ContainsKey(dependencyType))
                    throw DependencyResolutionException.CreateForProperty(
                        serviceType, dependencyType, "unknown");
            }
        }
    }

    private void InstantiateServices(IReadOnlyList<Type> sortedTypes)
    {
        // Use BuilderResolutionScope as adapter for IServiceFactory calls
        var scope = new BuilderResolutionScope(this);

        // First pass: Create all instances without applying property dependencies
        foreach (var serviceType in sortedTypes)
        {
            var serviceFactory = _registrations[serviceType];

            var instance = serviceFactory.CreateInstance(scope);
            _instances[serviceType] = instance;
        }

        // Second pass: Apply property dependencies to all instances
        foreach (var serviceType in sortedTypes)
        {
            var serviceFactory = _registrations[serviceType];
            var instance = _instances[serviceType];

            serviceFactory.ApplyProperties(instance, scope);
        }
    }
}
