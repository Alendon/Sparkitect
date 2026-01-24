namespace Sparkitect.Generator.Stateless;

/// <summary>
/// Model for a stateless function within a parent type
/// </summary>
/// <param name="MethodName">The C# method name</param>
/// <param name="Identifier">The string identifier from StatelessFunctionAttribute</param>
/// <param name="IdentifierPascal">PascalCase version of identifier for property names</param>
/// <param name="WrapperClassName">Generated wrapper class name</param>
/// <param name="SchedulingTypeName">Full type name of the scheduling implementation (e.g., OnCreateScheduling)</param>
/// <param name="RegistryTypeName">Full type name of the associated registry</param>
/// <param name="ContextTypeName">Full type name of the context type</param>
/// <param name="Parameters">Parameters to inject via DI</param>
/// <param name="SchedulingParams">Constructor parameters for the scheduling type, with matched attributes</param>
public record StatelessFunctionModel(
    string MethodName,
    string Identifier,
    string IdentifierPascal,
    string WrapperClassName,
    string SchedulingTypeName,
    string RegistryTypeName,
    string ContextTypeName,
    ImmutableValueArray<StatelessParameterModel> Parameters,
    ImmutableValueArray<SchedulingConstructorParam> SchedulingParams);

/// <summary>
/// Model for a parameter of a stateless function
/// </summary>
public record StatelessParameterModel(
    int Index,
    string ParameterType,
    bool IsOptional);

/// <summary>
/// Represents a constructor parameter of a scheduling type.
/// The SG analyzes the scheduling constructor and matches attributes from the method.
/// </summary>
/// <param name="AttributeTypeName">Full type name of the attribute (non-generic base, e.g., "global::Sparkitect.GameState.OrderAfterAttribute")</param>
/// <param name="IsNullable">If true, attribute is optional (? modifier)</param>
/// <param name="IsArray">If true, multiple instances allowed ([] modifier)</param>
/// <param name="Instances">Attribute instances found on the method matching this parameter type</param>
public record SchedulingConstructorParam(
    string AttributeTypeName,
    bool IsNullable,
    bool IsArray,
    ImmutableValueArray<SchedulingAttributeInstance> Instances);

/// <summary>
/// Represents a single attribute instance applied to a method.
/// Contains enough information to exactly reproduce the attribute construction.
/// </summary>
/// <param name="GenericArgs">Generic type arguments (e.g., ["global::MyModule.MyFunc"])</param>
/// <param name="CtorArgs">Raw literal constructor arguments</param>
public record SchedulingAttributeInstance(
    ImmutableValueArray<string> GenericArgs,
    ImmutableValueArray<string> CtorArgs);

/// <summary>
/// Model for a parent type containing stateless functions
/// </summary>
/// <param name="ParentTypeName">The containing type name</param>
/// <param name="ParentNamespace">The namespace of the containing type</param>
/// <param name="ParentIdentificationExpr">Expression to get parent's Identification (e.g., "ModuleName.Identification")</param>
/// <param name="Functions">All stateless functions in this parent</param>
public record StatelessParentModel(
    string ParentTypeName,
    string ParentNamespace,
    string ParentIdentificationExpr,
    ImmutableValueArray<StatelessFunctionModel> Functions);

/// <summary>
/// Grouping of functions by registry for registration output
/// </summary>
public record StatelessRegistryGroupModel(
    string RegistryTypeName,
    string RegistryKey,
    ImmutableValueArray<StatelessFunctionModel> Functions);
