using System.Diagnostics.CodeAnalysis;

namespace Sparkitect.DI.Container;

// TODO @Codex. This interface/class name is probably absolutely wrong. Propose more fitting names, then rename and remove this todo.

public interface IFacadedCoreContainer : ICoreContainer
{
    TFacade ResolveFacaded<TFacade>() where TFacade : class;
    bool TryResolveFacaded<TFacade>([NotNullWhen(true)] out TFacade? facade) where TFacade : class;
    bool TryResolveFacaded(Type facadeType, out object? service);
}