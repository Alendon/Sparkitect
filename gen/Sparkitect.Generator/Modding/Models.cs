using System;

namespace Sparkitect.Generator.Modding;

public record RegistryModel(
    string TypeName,
    string Key,
    string ContainingNamespace,
    ImmutableValueArray<RegisterMethodModel> RegisterMethods,
    ImmutableValueArray<(string identifier, bool optional)> ResourceFiles);



public record MethodRegistrationEntry(
    string RegistryNamespace,
    string RegistryTypeName,
    string RegistryMethodName,
    string Id,
    string ProviderContainingType,
    string ProviderMethodName,
    ImmutableValueArray<(string paramType, bool isNullable)> Parameters);

public record TypeRegistrationEntry(
    string RegistryNamespace,
    string RegistryTypeName,
    string RegistryMethodName,
    string Id,
    string ProvidedTypeFullName);

public record FileRegistrationEntry(string MetadataClass, string Id,  ImmutableValueArray<(string fileId, string fileName)> Files);


/// <summary>
/// Model to represent a registry method
/// </summary>
/// <param name="FunctionName">The name of the method. Names must be unique inside one Registry</param>
/// <param name="PrimaryParameterKind">The kind of the primary parameter</param>
/// <param name="Constraint">Direct constraints</param>
/// <param name="TypeConstraint">Affecting type constraints. May be empty</param>
public record RegisterMethodModel(string FunctionName, PrimaryParameterKind PrimaryParameterKind, TypeConstraintFlag Constraint, ImmutableValueArray<string> TypeConstraint);

/// <summary>
/// The kind of the primary parameter of a registry method
/// </summary>
public enum PrimaryParameterKind
{
    /// <summary>
    /// No parameter supplied
    /// </summary>
    /// <remarks>Resource file registration</remarks>
    None = 1,
    
    /// <summary>
    /// Registry method accept a value (class/struct). The TypeConstraint list contains a single value  (represents the parameter type)
    /// </summary>
    /// <remarks>Method registration</remarks>
    Value = 2,
    
    /// <summary>
    /// Registry method accept a value based on a generic type.
    /// </summary>
    /// <remarks>Method registration</remarks>
    GenericValue = 3,
    
    /// <summary>
    /// Registry method accept just a singl
    /// </summary>
    /// <remarks>Type registration</remarks>
    Type = 4
}

[Flags]
public enum TypeConstraintFlag
{
    None = 0,
    ReferenceType = 1 << 0,
    ValueType = 1 << 1,
    AllowRefLike = 1 << 2,
    Unmanaged = 1 << 3,
    NotNull = 1 << 4,
    ParameterlessConstructor = 1 << 5
}
