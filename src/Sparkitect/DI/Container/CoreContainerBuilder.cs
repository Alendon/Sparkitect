using System.Diagnostics.CodeAnalysis;
using QuikGraph;
using QuikGraph.Algorithms;
using QuikGraph.Algorithms.TopologicalSort;
using Sparkitect.DI.Exceptions;

namespace Sparkitect.DI.Container;

internal class CoreContainerBuilder : ICoreContainerBuilder
{
    private readonly Dictionary<Type, IServiceFactory> _registrations = [];
    private readonly Dictionary<Type, object> _instances = [];
    private readonly ICoreContainer? _parentContainer;
    private readonly IReadOnlyDictionary<Type, Type>? _facadeMap;

    public CoreContainerBuilder(ICoreContainer? parentContainer)
    {
        _parentContainer = parentContainer;
        _facadeMap = null;
    }

    public CoreContainerBuilder(ICoreContainer? parentContainer, IReadOnlyDictionary<Type, Type>? facadeMap)
    {
        _parentContainer = parentContainer;
        _facadeMap = facadeMap;
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
        ValidateDependencyGraph();
        InstantiateServices();

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

    private void ValidateDependencyGraph()
    {
        var graph = new AdjacencyGraph<Type, Edge<Type>>();

        // Add vertices for all service types
        foreach (var serviceType in _registrations.Keys)
        {
            graph.AddVertex(serviceType);
        }

        // Add edges for required constructor dependencies only
        foreach (var serviceFactory in _registrations.Values)
        {
            var serviceType = serviceFactory.ServiceType;

            foreach (var (dependencyType, isOptional) in serviceFactory.GetConstructorDependencies())
            {
                var resolvedDependencyType = dependencyType;
                if (_facadeMap?.TryGetValue(dependencyType, out var mappedType) == true)
                    resolvedDependencyType = mappedType;

                if(_parentContainer?.TryResolve(resolvedDependencyType, out _) is true) continue;

                if (isOptional)
                {
                    if (_registrations.ContainsKey(resolvedDependencyType))
                    {
                        graph.AddEdge(new Edge<Type>(resolvedDependencyType, serviceType));
                    }

                    continue;
                }

                if (!_registrations.ContainsKey(resolvedDependencyType))
                    throw new DependencyResolutionException(
                        $"Missing dependency {resolvedDependencyType.Name} for {serviceType.Name}");

                graph.AddEdge(new Edge<Type>(resolvedDependencyType, serviceType));
            }
        }

        // Check for cycles in constructor dependencies
        if (!graph.IsDirectedAcyclicGraph())
        {
            throw new CircularDependencyException("Circular dependency detected in the container");
        }

        // Validate property dependencies separately - just check if they exist
        foreach (var serviceFactory in _registrations.Values)
        {
            var serviceType = serviceFactory.ServiceType;

            foreach (var (dependencyType, isOptional) in serviceFactory.GetPropertyDependencies())
            {
                if (isOptional)
                    continue;

                var resolvedDependencyType = dependencyType;
                if (_facadeMap?.TryGetValue(dependencyType, out var mappedType) == true)
                    resolvedDependencyType = mappedType;

                if(_parentContainer?.TryResolve(resolvedDependencyType, out _) is true) continue;

                if (!_registrations.ContainsKey(resolvedDependencyType))
                    throw new DependencyResolutionException(
                        $"Missing property dependency {resolvedDependencyType.Name} for {serviceType.Name}");
            }
        }
    }

    private void InstantiateServices()
    {
        var graph = new AdjacencyGraph<Type, Edge<Type>>();

        // Add vertices for all service types
        foreach (var serviceType in _registrations.Keys)
        {
            graph.AddVertex(serviceType);
        }

        // Add edges for constructor dependencies that exist in the container
        foreach (var serviceFactory in _registrations.Values)
        {
            var serviceType = serviceFactory.ServiceType;

            foreach (var (dependencyType, _) in serviceFactory.GetConstructorDependencies())
            {
                var resolvedDependencyType = dependencyType;
                if (_facadeMap?.TryGetValue(dependencyType, out var mappedType) == true)
                    resolvedDependencyType = mappedType;

                if (_registrations.ContainsKey(resolvedDependencyType))
                {
                    graph.AddEdge(new Edge<Type>(resolvedDependencyType, serviceType));
                }
            }
        }

        // Topologically sort based on constructor dependencies
        var algorithm = new TopologicalSortAlgorithm<Type, Edge<Type>>(graph);
        algorithm.Compute();
        var sortedTypes = algorithm.SortedVertices.ToList();

        // First pass: Create all instances without applying property dependencies
        foreach (var serviceType in sortedTypes)
        {
            var serviceFactory = _registrations[serviceType];

            var instance = serviceFactory.CreateInstance(this, _facadeMap ?? new Dictionary<Type, Type>());
            _instances[serviceType] = instance;
        }

        // Second pass: Apply property dependencies to all instances
        foreach (var serviceType in sortedTypes)
        {
            var serviceFactory = _registrations[serviceType];
            var instance = _instances[serviceType];

            serviceFactory.ApplyProperties(instance, this, _facadeMap ?? new Dictionary<Type, Type>());
        }
    }
}