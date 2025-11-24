namespace Sparkitect.DI.Exceptions;

/// <summary>
/// Thrown when a circular dependency is detected during container construction.
/// Circular dependencies occur when two or more services depend on each other directly or through a chain.
/// </summary>
public class CircularDependencyException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CircularDependencyException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the circular dependency error.</param>
    public CircularDependencyException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircularDependencyException"/> class with a specified error message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the circular dependency error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public CircularDependencyException(string message, Exception innerException)
        : base(message, innerException) { }
}