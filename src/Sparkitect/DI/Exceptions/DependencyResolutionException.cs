namespace Sparkitect.DI.Exceptions;

/// <summary>
/// Thrown when a dependency cannot be resolved from the DI container.
/// This typically occurs when a required service is not registered or a dependency is missing.
/// </summary>
public class DependencyResolutionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyResolutionException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the resolution error.</param>
    public DependencyResolutionException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyResolutionException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the resolution error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public DependencyResolutionException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>
    /// Creates a new <see cref="DependencyResolutionException"/> for a missing dependency with a formatted error message.
    /// </summary>
    /// <typeparam name="TService">The service type that has the missing dependency.</typeparam>
    /// <typeparam name="TDependency">The dependency type that could not be resolved.</typeparam>
    /// <returns>A new exception instance with a descriptive error message.</returns>
    public static DependencyResolutionException Create<TService, TDependency>()
        => new($"Missing Dependency {typeof(TDependency).FullName} for {typeof(TService).FullName}");

    /// <summary>
    /// Creates an exception for a missing constructor parameter dependency.
    /// </summary>
    /// <typeparam name="TService">The service type that has the missing dependency.</typeparam>
    /// <typeparam name="TDependency">The dependency type that could not be resolved.</typeparam>
    /// <param name="parameterName">The name of the constructor parameter.</param>
    /// <returns>A new exception instance with a descriptive error message.</returns>
    public static DependencyResolutionException CreateForConstructor<TService, TDependency>(string parameterName)
        => new($"Cannot resolve {typeof(TDependency).FullName} for constructor parameter '{parameterName}' of {typeof(TService).FullName}");

    /// <summary>
    /// Creates an exception for a missing property dependency.
    /// </summary>
    /// <typeparam name="TService">The service type that has the missing dependency.</typeparam>
    /// <typeparam name="TDependency">The dependency type that could not be resolved.</typeparam>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>A new exception instance with a descriptive error message.</returns>
    public static DependencyResolutionException CreateForProperty<TService, TDependency>(string propertyName)
        => new($"Cannot resolve {typeof(TDependency).FullName} for property '{propertyName}' of {typeof(TService).FullName}");

    /// <summary>
    /// Creates an exception for a missing constructor parameter dependency (non-generic version).
    /// </summary>
    /// <param name="serviceType">The service type that has the missing dependency.</param>
    /// <param name="dependencyType">The dependency type that could not be resolved.</param>
    /// <param name="parameterName">The name of the constructor parameter.</param>
    /// <returns>A new exception instance with a descriptive error message.</returns>
    public static DependencyResolutionException CreateForConstructor(Type serviceType, Type dependencyType, string parameterName)
        => new($"Cannot resolve {dependencyType.FullName} for constructor parameter '{parameterName}' of {serviceType.FullName}");

    /// <summary>
    /// Creates an exception for a missing property dependency (non-generic version).
    /// </summary>
    /// <param name="serviceType">The service type that has the missing dependency.</param>
    /// <param name="dependencyType">The dependency type that could not be resolved.</param>
    /// <param name="propertyName">The name of the property.</param>
    /// <returns>A new exception instance with a descriptive error message.</returns>
    public static DependencyResolutionException CreateForProperty(Type serviceType, Type dependencyType, string propertyName)
        => new($"Cannot resolve {dependencyType.FullName} for property '{propertyName}' of {serviceType.FullName}");
}