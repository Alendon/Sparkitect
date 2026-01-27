using Microsoft.CodeAnalysis;

namespace Sparkitect.Generator.Modding.Analyzers;

// Category 02: Registry/Modding Diagnostics (SPARK02XX)
// - Registry shape validation (01-09)
// - Registry method validation (10-19)
// - Provider usage validation (20-29)
// - Registration validation (30-39)
// - YAML resource validation (42-49)
// - Property name validation (50-59)

public static class RegistryDiagnostics
{
    private const string Category = "Sparkitect";

    // Shape / Registry type-level (01-09)
    public static readonly DiagnosticDescriptor RegistryRequiresInterface =
        new("SPARK0201", "[Registry] requires IRegistry",
            "Type '{0}' has [Registry] but doesn't implement IRegistry. Add ': IRegistry' to the type declaration.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor RegistryMissingIdentifier =
        new("SPARK0202", "Registry must specify Identifier",
            "Registry '{0}' has no Identifier. Add Identifier = \"your_key\" to the [Registry] attribute.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor RegistryMustBeTopLevelInNamespace =
        new("SPARK0203", "Registry must be top-level in a namespace",
            "Registry '{0}' must be top-level in a namespace. Move it out of nested classes and ensure it's in a namespace (not global).",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicateCategoryKey =
        new("SPARK0204", "Duplicate registry category Identifier",
            "Multiple registries declare the same category Identifier '{0}'. Rename one registry's Identifier to be unique.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicateRegistryTypeNames =
        new("SPARK0205", "Duplicate registry type names across namespaces",
            "Multiple registries named '{0}' exist in different namespaces. Rename one to avoid confusion.",
            Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor CategoryIdentifierNotSnakeCase =
        new("SPARK0206", "Category Identifier must be snake_case",
            "Registry Identifier '{0}' must be snake_case. Use only lowercase letters, digits, and underscores.",
            Category, DiagnosticSeverity.Error, true);

    // Registry method validation (10-19)
    public static readonly DiagnosticDescriptor RegistryMethodOutsideRegistry =
        new("SPARK0210", "[RegistryMethod] only inside registries",
            "[RegistryMethod] must be inside a registry class. Move the method to a class with [Registry] attribute that implements IRegistry.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor InvalidRegistryMethodSignature =
        new("SPARK0211", "Invalid [RegistryMethod] signature",
            "Registry method '{0}' has invalid signature. Must match one of the allowed signatures (see docs).",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor TooManyTypeParameters =
        new("SPARK0212", "Registry method has too many type parameters",
            "Registry method '{0}' has too many type parameters. Use at most one type parameter.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor GenericValueMismatch =
        new("SPARK0213", "Generic value method mismatch",
            "Registry method '{0}' declares a type parameter but second parameter doesn't match. Use the generic type as the second parameter.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor FirstParameterMustBeIdentification =
        new("SPARK0214", "First parameter must be Identification",
            "Registry method '{0}' must have Identification as first parameter. Change first parameter to 'Sparkitect.Modding.Identification'.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicateRegistryMethodName =
        new("SPARK0215", "Duplicate registry method name",
            "Duplicate registry method name '{0}' in '{1}'. Rename one method - names must be unique per registry.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor UseResourceFileMissingKey =
        new("SPARK0216", "UseResourceFile missing Key",
            "[UseResourceFile] on '{0}' has no Key. Add Key = \"your_key\" to the attribute.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DuplicateResourceFileKey =
        new("SPARK0217", "Duplicate resource file key",
            "Duplicate resource file key '{0}' on registry '{1}'. Rename one key to be unique.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor MultiplePrimaryResourceFiles =
        new("SPARK0218", "Multiple primary resource files",
            "Registry '{0}' has multiple resource files marked as Primary. Set Primary = true on only one [UseResourceFile].",
            Category, DiagnosticSeverity.Error, true);

    // Provider usage validation (20-29)
    public static readonly DiagnosticDescriptor ProviderMissingId =
        new("SPARK0220", "Provider attribute requires id",
            "Provider attribute '{0}' has no id. Add the registration id as the first string argument.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ProviderMemberMustBeStatic =
        new("SPARK0221", "Provider member must be static",
            "Member '{0}' must be static to be used with registry provider attributes. Add the 'static' modifier.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor UnknownRegistryReference =
        new("SPARK0222", "Referenced registry not discoverable",
            "Registry '{0}' referenced by provider attribute is not available. Check the registry type exists and is accessible.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor UnknownRegistryMethod =
        new("SPARK0223", "Unknown registry method",
            "Registry '{0}' does not declare a registry method named '{1}'. Check the method name spelling or add [RegistryMethod] to the target.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ProviderKindMismatch =
        new("SPARK0224", "Provider kind does not match registry method",
            "Attribute '{0}' is used on a {1} but targets registry method '{2}' of incompatible kind. Use the matching provider attribute.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor ProviderReturnTypeIncompatible =
        new("SPARK0225", "Provider return type incompatible",
            "Member '{0}' returns '{1}', incompatible with registry method '{2}'. Change the return type to match.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor TypeDoesNotSatisfyConstraints =
        new("SPARK0226", "Type does not satisfy generic constraints",
            "Type '{0}' does not satisfy constraints of generic registry method '{1}'. Ensure the type meets all constraints.",
            Category, DiagnosticSeverity.Error, true);

    // Registration validation (30-39)
    public static readonly DiagnosticDescriptor DuplicateRegistrationId =
        new("SPARK0230", "Duplicate registration id",
            "Duplicate registration id '{0}' for registry '{1}'. Rename one registration - each id must be unique.",
            Category, DiagnosticSeverity.Error, true, customTags: WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor RegistrationIdNotSnakeCase =
        new("SPARK0231", "Registration id must be snake_case",
            "Registration id '{0}' must be snake_case. Use only lowercase letters, digits, and underscores.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor DiParameterShouldBeAbstract =
        new("SPARK0232", "Prefer abstract/interface DI parameters",
            "Parameter '{0}' of type '{1}' should be an interface or abstract class for DI resolution. Change to an interface or mark nullable.",
            Category, DiagnosticSeverity.Warning, true);

    // YAML resource validation (42-49)
    public static readonly DiagnosticDescriptor YamlUnknownRegistryKey =
        new("SPARK0242", "Unknown registry/method in YAML key",
            "YAML key '{0}' references unknown registry/method. Check the key spelling matches a registry method.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor YamlUnknownFileKey =
        new("SPARK0243", "Unknown file key in YAML",
            "File key '{0}' in '{1}' is not declared by registry '{2}'. Valid keys: {3}. Add [UseResourceFile] or fix the key name.",
            Category, DiagnosticSeverity.Warning, true);

    public static readonly DiagnosticDescriptor YamlMissingRequiredFileKey =
        new("SPARK0244", "Missing required file key in YAML",
            "Resource entry '{0}' in '{1}' is missing required file key '{2}'. Add the '{2}' field to the entry.",
            Category, DiagnosticSeverity.Error, true);

    public static readonly DiagnosticDescriptor YamlDuplicateId =
        new("SPARK0245", "Duplicate id in YAML",
            "Duplicate id '{0}' for registry '{1}' in resource files. Rename one id to be unique.",
            Category, DiagnosticSeverity.Error, true);

    // Property name validation (50-59)
    public static readonly DiagnosticDescriptor DuplicateNormalizedPropertyName =
        new("SPARK0250", "Duplicate property name after normalization",
            "Entries '{0}' and '{1}' normalize to the same property name '{2}' in registry '{3}'. Rename one entry.",
            Category, DiagnosticSeverity.Error, true, customTags: WellKnownDiagnosticTags.CompilationEnd);
}
