namespace Sparkitect.DI.Ordering;

/// <summary>
/// Interface for types that apply entrypoint ordering constraints.
/// Implemented by attributes placed on entrypoint classes.
/// </summary>
public interface IEntrypointOrdering
{
    /// <summary>
    /// Applies ordering constraints to the builder for the current entrypoint type.
    /// </summary>
    /// <param name="builder">The ordering builder that collects directed edges.</param>
    void ApplyOrdering(IEntrypointOrderingBuilder builder);
}
