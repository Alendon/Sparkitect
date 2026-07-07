using System;
using Sparkitect.Generator.DI.Pipeline;

namespace Sparkitect.Generator.Modding;

/// <summary>
/// Wrapper record that pairs a RegistryModel with its DI pipeline factory data.
/// Keeps RegistryModel clean of DI pipeline types while carrying factory data alongside.
/// </summary>
internal record RegistryWithFactory(
    RegistryModel Registry,
    FactoryWithRegistration FactoryData,
    ImmutableValueArray<FacadeMetadataModel> FacadeMetadata);

public record RegistryModel(
    string TypeName,
    string Key,
    string ContainingNamespace,
    bool IsExternal,
    ImmutableValueArray<RegisterMethodModel> RegisterMethods,
    ImmutableValueArray<(string Key, bool Required, bool Primary)> ResourceFiles,
    string? DeclaringSgNamespace = null,
    string? OwningModuleFullName = null);

public record FileRegistrationEntry(
    string RegistryClass,
    string MethodName,
    string Id,
    ImmutableValueArray<(string fileId, string fileName)> Files,
    string? SourcePath = null,
    int SourceLine = 0,
    int SourceColumn = 0);


/// <summary>
/// A constructed-generic constraint on one of a register method's type parameters that references
/// another type parameter. This is the resolution map the constraint-guided walk (Plan 04) consumes.
/// </summary>
/// <param name="TypeParameterName">The type parameter the constraint is declared on.</param>
/// <param name="ConstraintOpenDefinitionFqn">The constraint's open generic definition FQN (e.g. <c>RelationShip&lt;&gt;</c>).</param>
/// <param name="ArgTypeParameterNames">Per constraint-argument position, the referenced method type-parameter
/// name, or empty string when that position is a concrete (non-type-parameter) type.</param>
public record RegisterConstraintRef(
    string TypeParameterName,
    string ConstraintOpenDefinitionFqn,
    ImmutableValueArray<string> ArgTypeParameterNames);

/// <summary>
/// Model to represent a registry method
/// </summary>
/// <param name="FunctionName">The name of the method. Names must be unique inside one Registry</param>
/// <param name="PrimaryParameterKind">The kind of the primary parameter</param>
/// <param name="Constraint">Direct constraints</param>
/// <param name="TypeConstraint">Affecting type constraints. May be empty</param>
/// <param name="KeyedFactoryMarkerTBase">Keyed-factory marker base type, when present</param>
/// <param name="KeyedFactoryMarkerTKey">Keyed-factory marker key type, when present</param>
/// <param name="TypedIdentificationTypeParameterName">Name of the type parameter annotated with
/// <c>[TypedIdentification]</c>, or null when no type parameter opts in.</param>
/// <param name="ResolvedTypeArguments">The fully-resolved, closed type-argument list (D-08 normalized
/// representation). Empty until Plan 04 populates it.</param>
/// <param name="TypeParameterNames">The method's type parameters in declaration order; index 0 is the
/// type-source anchor (the slot the registered type fills).</param>
/// <param name="ConstraintRefs">One entry per constructed-generic constraint that references another
/// type parameter — the resolution map for Plan 04's pure-string walk.</param>
/// <param name="ValueParameterGeneric">The value parameter's constructed-generic structure, for
/// value-source resolution against the provider return type; null for bare-<c>T</c> or non-generic
/// value params.</param>
public record RegisterMethodModel(
    string FunctionName,
    PrimaryParameterKind PrimaryParameterKind,
    TypeConstraintFlag Constraint,
    ImmutableValueArray<string> TypeConstraint,
    string? KeyedFactoryMarkerTBase = null,
    string? KeyedFactoryMarkerTKey = null,
    string? TypedIdentificationTypeParameterName = null,
    ImmutableValueArray<string> ResolvedTypeArguments = default!,
    ImmutableValueArray<string> TypeParameterNames = default!,
    ImmutableValueArray<RegisterConstraintRef> ConstraintRefs = default!,
    RegisterConstraintRef? ValueParameterGeneric = null);

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
    /// Registry method accepts a value (class/struct) supplying the registration's data — the value
    /// source. Covers both bare-value (0 type parameters) and generic-value (1..N type parameters,
    /// inferred from the passed argument) register methods. The TypeConstraint list contains a single
    /// value (represents the parameter type).
    /// </summary>
    /// <remarks>Method registration</remarks>
    Value = 2,

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
