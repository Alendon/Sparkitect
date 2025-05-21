namespace Sparkitect.DI.Exceptions;

public class CircularDependencyException : Exception
{
    public CircularDependencyException(string message) : base(message) { }
    public CircularDependencyException(string message, Exception innerException) 
        : base(message, innerException) { }
}