using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Serilog;
using Sparkitect.DI.Container;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.Modding;

namespace Sparkitect.GameState;

/// <summary>
/// Scaffolding implementation. The full logic will be wired with SG, registries, and DI.
/// </summary>
[CreateServiceFactory<IGameStateManager>]
[FacadeToRegistry<IGameStateManagerRegistryFacade>]
internal sealed class GameStateManager : IGameStateManager, IGameStateManagerRegistryFacade
{

    public void EnterRootState(ICoreContainer coreContainer)
    {
        CurrentCoreContainer = coreContainer;

        throw new NotImplementedException();
    }

    public ICoreContainer CurrentCoreContainer { get; private set; } = null!;

    public void Request(Identification stateId, object? payload = null)
    {
        throw new NotImplementedException();
    }

    public void AddStateModule<TStateModule>(Identification id) where TStateModule : class, IStateModule
    {
        throw new NotImplementedException();
    }

    public void RemoveStateModule(Identification id)
    {
        throw new NotImplementedException();
    }

    public void AddStateDescriptor<TStateDescriptor>(Identification id) where TStateDescriptor : class, IStateDescriptor
    {
        throw new NotImplementedException();
    }

    public void RemoveStateDescriptor(Identification id)
    {
        throw new NotImplementedException();
    }
}
