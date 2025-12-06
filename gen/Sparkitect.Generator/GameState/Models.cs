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
/// <param name="MethodName">The C# method name</param>
/// <param name="FunctionKey">The string key value from StateFunctionAttribute</param>
/// <param name="KeyExpression">The expression to reference the key (either generated const or existing const reference)</param>
/// <param name="GenerateConstField">Whether to generate a const field for this key</param>
/// <param name="Schedule">When this function should execute</param>
/// <param name="Parameters">Parameters to inject</param>
/// <param name="OrderingConstraints">Ordering constraints relative to other functions</param>
public record StateFunctionModel(
    string MethodName,
    string FunctionKey,
    string KeyExpression,
    bool GenerateConstField,
    StateMethodSchedule Schedule,
    ImmutableValueArray<StateParameterModel> Parameters,
    ImmutableValueArray<OrderingConstraint> OrderingConstraints);

/// <summary>
/// Model for a parameter of a state function
/// </summary>
public record StateParameterModel(
    string ParameterName,
    string ParameterType,
    bool IsOptional);

/// <summary>
/// Represents an ordering constraint on a state function
/// </summary>
/// <param name="Direction">Before or After</param>
/// <param name="TargetKeyExpression">The expression referencing the target key (e.g., global::Namespace.Module.Key_Field)</param>
/// <param name="TargetModuleOrStateType">The target module/state type expression if cross-module ordering, null for same-module</param>
public record OrderingConstraint(
    OrderingDirection Direction,
    string TargetKeyExpression,
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
    OnCreate,
    OnDestroy,
    OnFrameEnter,
    OnFrameExit
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
    string KeyExpression,
    string WrapperTypeName,
    string ScheduleName);

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
/// <param name="BeforeParentId">Expression for the module/state identification of the before-function</param>
/// <param name="BeforeMethodKeyExpression">Expression referencing the before-function's key const</param>
/// <param name="AfterParentId">Expression for the module/state identification of the after-function</param>
/// <param name="AfterMethodKeyExpression">Expression referencing the after-function's key const</param>
public record OrderingRelationship(
    string BeforeParentId,
    string BeforeMethodKeyExpression,
    string AfterParentId,
    string AfterMethodKeyExpression);

/// <summary>
/// Model for state service mapping output
/// </summary>
public record StateServiceMappingModel(
    string Namespace,
    string ClassName,
    ImmutableValueArray<ServiceFacadeMapping> Mappings);

/// <summary>
/// Model for a single service-to-facade mapping
/// </summary>
public record ServiceFacadeMapping(
    string InterfaceType,
    string ServiceType,
    ImmutableValueArray<string> FacadeTypes);

/// <summary>
/// Model for state module service configurator output
/// </summary>
public record StateModuleServiceConfiguratorModel(
    string Namespace,
    string ClassName,
    string ModuleTypeName,
    string ModuleTypeFullName,
    ImmutableValueArray<StateServiceFactory> ServiceFactories);

/// <summary>
/// Model for a state service factory registration
/// </summary>
public record StateServiceFactory(
    string FactoryTypeName);