using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Serilog;
using OneOf;
using OneOf.Types;
using Sparkitect.DI.Container;
using Sparkitect.DI.GeneratorAttributes;
using Sparkitect.Modding;
using Sparkitect.DI;
using Sparkitect.CompilerGenerated.IdExtensions;
using Sparkitect.Modding.IDs;

namespace Sparkitect.GameState;

/// <summary>
/// Scaffolding implementation. The full logic will be wired with SG, registries, and DI.
/// </summary>
[CreateServiceFactory<IGameStateManager>]
internal sealed class GameStateManager : IGameStateManager, IGameStateManagerRegistryFacade
{
    /*
     * We need to store in here, first all informations related to modules and states by Identification, to be able to
     * construct states at runtime
     *
     * As well as a Stack of the current active states, which act hierarchically
     */

    public ICoreContainer CurrentCoreContainer => throw new NotImplementedException();
    
    internal required IModManager ModManager { get; init; }

    public void EnterRootState(ICoreContainer coreContainer)
    {
        Log.Information("Entering Root State");
    }


    public void Request(Identification stateId, object? payload = null)
    {
        //Function needs probably a different (generic) signature for a type safe payload
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
