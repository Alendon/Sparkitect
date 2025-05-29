namespace Sparkitect.DI.Exceptions;

public class DependencyResolutionException : Exception
{
    public DependencyResolutionException(string message) : base(message) { }
    public DependencyResolutionException(string message, Exception innerException) 
        : base(message, innerException) { }

    public static DependencyResolutionException Create<TService, TDependency>()
        => new($"Missing Dependency {typeof(TDependency).FullName} for {typeof(TService).FullName}");
}