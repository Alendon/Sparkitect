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
}