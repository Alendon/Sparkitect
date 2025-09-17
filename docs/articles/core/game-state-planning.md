# Game State Planning

This document is for planning the game state system, to be implemented into the Sparkitect Engine.

##  Goal
The goal of the game state system, is to provide mechanic with which developers can define states,
in which the game can be, with each of them having different configurations/services exposed.
And to provide an easy, reliable and extendable way of working with them.

## Atomic Components
The following components are the atomic elements in the game state system.
States are not necessarily directly composed of them, this would be way to tedious,
Instead we need to add some kind of "composite component", which can be viewed as a building block containing multiple atomic components.

### State Bound Service

A State Bound Service, is a DI Service which is only instantiated in the Container Hierarchy if an GameState requires it.
Currently the Engine only supports these kind of DI Services as Singletons.
A State Bound Service can expose a "Facade" to Features and Transitions
This allows Features and Transitions to access logic of State Bound Services, which is not exposed otherwise.

Sample Implementation of how an implementation can be marked as a State Bound Service
This sample is missing how the association between the Service and the Game State is done.
```csharp
//Defining a State Bound Service with only a public API
[StateBoundService<IMyServiceExposed>]
public class MyService : IMyServiceExposed
{
    /* Implementation */
}

//Defining a State Bound Service with an facaded API
//Transitions/Features can query both IMyServiceExposed and IMyServiceFacaded
[StateBoundService<IMyServiceExposed, IMyServiceFacaded>]
public class MyService : IMyServiceExposed, IMyServiceFacaded
{
    /* Implementation */
}
```

### Feature

Features are the components which compose the main loop of game states.
Features themself are designed to be stateless, they access State through the exposed DI Container.

In the simplest Form, a Feature is a static method, containing the DI Parameters as Method Parameters.
A Source Generator will be created, to create boilerplate code for resolving the parameters and calling the function,
to avoid computationally intensive code like Reflection.

Sample of how a Feature is implemented:

```csharp
[RegisterFeature("sample")]
internal static void ProcessMyService(IMyServiceExposed publicApi, IMyServiceFacaded facededApi)
{
    publicApi.DoSomethingPublic();
    facededApi.DoFeatureRelatedWork();
}
```

By using a static function, the function itself could be placed directly in the MyService class if wanted,
without creating errors, if the implementation is replaced with a new one.

A Source Generator would later create a wrapper class, which can be without additionally cost instantiated by the
State Management. This Wrapper Class would fetch all service objects in its initialization function.
For the actually Feature Invocation, the cached service objects can directly be used without cost.

### Transition

Transitions are components, which executes when the Game State is switching or updating.
Transitions in its simplest form are, like Features, just static methods.
They can be configured to trigger at different kind of state changes.

A Transition is always related to one or multiple Services which it "manages".
A Transition is not directly related to a specific State or Feature.

## State Hierarchy

States have an enforced hierarchy by the DI System.
The DI System only allows creating new Child Containers or fully disposing containers.
It is not possible to modify an existing container.

This places a critical restriction on the Hierarchy of the Services used in States.

Sample:
```
- RootState (Core Engine Components)
    - DesktopGameState (Core Engine Components, Rendering Services)
        - MainMenuState (Core Engine Components, Rendering Services, Main Manu Services)
        - LocalGame (Core Engine Components, Rendering Services, Game Services)
        - Client Game (Core Engine Components, Rendering Services, Game Services, Networking Services)
    - Server Game State (Core Engine Components, Game Services, Networking Services)
```

This Hierarchy does not include Features and Transitions.
As Features and Transitions are stateless themself, they get there State by the current Set of Services which are injected each call.
The Wrapper Classes of Features and Transitions are freshly initialized when Service Changes are done.

In the Hierarchy of the sample, all Child States are always just extending the Parent State.
Assuming the MainMenuState is the current State, to start a local game, 
a request is issued to the Game State System, to switch to LocalGame.
The Transition Path between Main Menu and Local Game is through the Desktop Game State.
EG first the Main Menu Services are teared down. 
For this we assume a Transition exists, which is defined to manage the Main Menu Services
and to shut them gracefully down.
Afterwards the current Container is disposed and the Hierarchy effectively "points to" the Desktop Game State.
Then the Child Container specific to the Local Game State is instantiated, and the associated Transitions for the new Services executed.
Afterwards the Main Loop Feature execution for the Local Game Starts.


