namespace Sparkitect.Generator.GameState;

/// <summary>
/// Model for a state module and its contained state functions
/// </summary>
public record StateModuleModel(
    string ModuleTypeName,
    string ModuleNamespace,
    string ModuleIdentification,
    ImmutableValueArray<StateFunctionModel> Functions,
    ImmutableValueArray<string> ModuleOrderingBefore,
    ImmutableValueArray<string> ModuleOrderingAfter);

/// <summary>
/// Model for a state function within a module
/// </summary>
public record StateFunctionModel(
    string MethodName,
    string FunctionKey,
    StateMethodSchedule Schedule,
    ImmutableValueArray<StateParameterModel> Parameters,
    ImmutableValueArray<OrderingConstraint> OrderingConstraints);

/// <summary>
/// Model for a parameter of a state function
/// </summary>
public record StateParameterModel(
    string ParameterName,
    string ParameterType,
    bool IsOptional,
    bool IsFacade);

/// <summary>
/// Represents an ordering constraint on a state function
/// </summary>
public record OrderingConstraint(
    OrderingDirection Direction,
    string TargetKey,
    string? TargetModuleOrStateType);

/// <summary>
/// Direction of ordering constraint
/// </summary>
public enum OrderingDirection
{
    Before,
    After
}

/// <summary>
/// Schedule for when a state method should execute
/// </summary>
public enum StateMethodSchedule
{
    PerFrame,
    OnStateEnter,
    OnStateExit,
    OnModuleEnter,
    OnModuleExit
}

/// <summary>
/// Model for the complete state method association output
/// </summary>
public record StateMethodAssociationModel(
    string Namespace,
    string ClassName,
    ImmutableValueArray<StateMethodRegistration> Registrations);

/// <summary>
/// Model for a single state method registration
/// </summary>
public record StateMethodRegistration(
    string ModuleTypeName,
    string ModuleIdentification,
    string FunctionKey,
    string WrapperTypeName,
    StateMethodSchedule Schedule);

/// <summary>
/// Model for the state method ordering output
/// </summary>
public record StateMethodOrderingModel(
    string Namespace,
    string ClassName,
    ImmutableValueArray<OrderingRelationship> Orderings);

/// <summary>
/// Model for a single ordering relationship
/// </summary>
public record OrderingRelationship(
    string BeforeParentId,
    string BeforeMethodKey,
    string AfterParentId,
    string AfterMethodKey);