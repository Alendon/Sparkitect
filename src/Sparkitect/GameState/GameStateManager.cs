using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Serilog;
using OneOf;
using OneOf.Types;
using QuikGraph.Algorithms;
using Sparkitect.DI.Container;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.Modding;
using Sparkitect.DI;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Modding.IDs;

namespace Sparkitect.GameState;

/// <summary>
/// Manages game state transitions, module lifecycle, and main loop execution
/// </summary>
[CreateServiceFactory<IGameStateManager>]
internal sealed class GameStateManager : IGameStateManager, IGameStateManagerRegistryFacade
{
   

    /// <summary>
    /// Enter the Root Game State. To be called by the Engine Bootstrapper
    /// </summary>
    public void EnterRootState(ICoreContainer coreContainer)
    {
 
        
    }


    public ICoreContainer CurrentCoreContainer { get; } = null!;
    public void Request(Identification stateId, object? payload = null)
    {
        throw new NotImplementedException();
    }

    public void Shutdown()
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
