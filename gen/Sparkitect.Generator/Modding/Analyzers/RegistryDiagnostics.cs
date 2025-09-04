using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.Modding.Analyzers;

public static class RegistryDiagnostics
{
    private const string Category = "Sparkitect";

    // Shape / Registry type-level
    public static readonly DiagnosticDescriptor RegistryRequiresInterface =
        new("SPARK2001", "[Registry] requires IRegistry",
            "Type '{0}' is marked with [Registry] but does not implement Sparkitect.Modding.IRegistry",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor RegistryMissingIdentifier =
        new("SPARK2002", "Registry must specify Identifier",
            "Registry '{0}' must specify a non-empty Identifier on [Registry]",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor RegistryMustBeTopLevelInNamespace =
        new("SPARK2003", "Registry must be top-level in a namespace",
            "Registry '{0}' must be declared as a top-level type inside a namespace; global namespace and nested types are not supported",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicateCategoryKey =
        new("SPARK2004", "Duplicate registry category Identifier",
            "Multiple registries declare the same category Identifier '{0}'. Category keys must be unique.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicateRegistryTypeNames =
        new("SPARK2005", "Duplicate registry type names across namespaces",
            "Multiple registries named '{0}' exist in different namespaces. Prefer unique type names to avoid confusion.",
            Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor CategoryIdentifierNotSnakeCase =
        new("SPARK2006", "Category Identifier must be snake_case",
            "Registry Identifier '{0}' is invalid. Use snake_case (letters and underscores only).",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor RegistryMethodOutsideRegistry =
        new("SPARK2010", "[RegistryMethod] only inside registries",
            "[RegistryMethod] may only be applied to methods declared inside a type implementing IRegistry",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor InvalidRegistryMethodSignature =
        new("SPARK2011", "Invalid [RegistryMethod] signature",
            "Registry method '{0}' must match one of the allowed signatures",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor TooManyTypeParameters =
        new("SPARK2012", "Registry method has too many type parameters",
            "Registry method '{0}' must declare at most one type parameter",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor GenericValueMismatch =
        new("SPARK2013", "Generic value method mismatch",
            "Registry method '{0}' declares a single type parameter; the second parameter must be of that generic type",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor FirstParameterMustBeIdentification =
        new("SPARK2014", "First parameter must be Identification",
            "Registry method '{0}' must have 'Sparkitect.Modding.Identification' as its first parameter",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicateRegistryMethodName =
        new("SPARK2015", "Duplicate registry method name",
            "Duplicate registry method name '{0}' in '{1}'. Method names must be unique per registry.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor UseResourceFileMissingIdentifier =
        new("SPARK2016", "UseResourceFile missing Identifier",
            "[UseResourceFile] on '{0}' must specify a non-empty Identifier",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicateResourceFileIdentifier =
        new("SPARK2017", "Duplicate resource file identifier",
            "Duplicate resource file identifier '{0}' on registry '{1}'",
            Category, DiagnosticSeverity.Error, true);

    // Provider usage
    public static readonly DiagnosticDescriptor ProviderMissingId =
        new("SPARK2020", "Provider attribute requires id",
            "Registry provider attribute '{0}' must specify the first positional string id argument",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ProviderMemberMustBeStatic =
        new("SPARK2021", "Provider member must be static",
            "Member '{0}' referenced by registry provider attribute must be static",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor UnknownRegistryReference =
        new("SPARK2022", "Referenced registry not discoverable",
            "Registry '{0}' referenced by provider attribute is not available",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor UnknownRegistryMethod =
        new("SPARK2023", "Unknown registry method",
            "Registry '{0}' does not declare a registry method named '{1}'",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ProviderKindMismatch =
        new("SPARK2024", "Provider kind does not match registry method",
            "Attribute '{0}' is used on a {1} but targets a registry method '{2}' of incompatible kind",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ProviderReturnTypeIncompatible =
        new("SPARK2025", "Provider return type incompatible",
            "Member '{0}' returns '{1}', which is incompatible with parameter type of registry method '{2}'",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor TypeDoesNotSatisfyConstraints =
        new("SPARK2026", "Type does not satisfy generic constraints",
            "Type '{0}' does not satisfy constraints of generic registry method '{1}'",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicateRegistrationId =
        new("SPARK2030", "Duplicate registration id",
            "Duplicate registration id '{0}' for registry '{1}'. Each id must be unique.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor RegistrationIdNotSnakeCase =
        new("SPARK2031", "Registration id must be snake_case",
            "Registration id '{0}' is invalid. Use snake_case (letters and underscores only).",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DiParameterShouldBeAbstract =
        new("SPARK2032", "Prefer abstract/interface DI parameters",
            "Parameter '{0}' of type '{1}' should be an interface or abstract class for DI resolution, or be marked nullable",
            Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor DuplicateNormalizedPropertyName =
        new("SPARK2050", "Duplicate property name after normalization",
            "Entries '{0}' and '{1}' normalize to the same property name '{2}' in registry '{3}'",
            Category, DiagnosticSeverity.Error, true);

    // YAML resource files
    public static readonly DiagnosticDescriptor YamlEntryMissingId =
        new("SPARK2040", "Resource entry missing id",
            "Resource entry in '{0}' is missing 'id'",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor YamlFileXorFiles =
        new("SPARK2041", "Specify either 'file' or 'files'",
            "Resource entry '{0}' in '{1}' must specify either 'file' or 'files', not both",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor YamlUnknownRegistryKey =
        new("SPARK2042", "Unknown registry/method in YAML key",
            "YAML key '{0}' references registry/method that is not discoverable",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor YamlUnknownFileKey =
        new("SPARK2043", "Unknown file key in YAML",
            "File key '{0}' in '{1}' is not declared by registry '{2}'. Valid keys: {3}.",
            Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor YamlMissingRequiredFileKey =
        new("SPARK2044", "Missing required file key in YAML",
            "Resource entry '{0}' in '{1}' is missing required file key '{2}'",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor YamlDuplicateId =
        new("SPARK2045", "Duplicate id in YAML",
            "Duplicate id '{0}' for registry '{1}' in resource files",
            Category, DiagnosticSeverity.Error, true);
}

