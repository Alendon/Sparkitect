using Sparkitect.Modding;
using Sparkitect.Stateless;

namespace Sparkitect.GameState;

/// <summary>
/// Registry-facaded interface for registering stateless functions.
/// Called by StatelessFunctionRegistryBase during registration.
/// </summary>
public interface IStatelessFunctionRegistrar
{
    /// <summary>
    /// Registers a stateless function with its identification.
    /// </summary>
    /// <typeparam name="TStatelessFunction">The generated wrapper type implementing IStatelessFunction.</typeparam>
    /// <param name="id">The function's identification.</param>
    void AddFunction<TStatelessFunction>(Identification id) where TStatelessFunction : IStatelessFunction;
}
