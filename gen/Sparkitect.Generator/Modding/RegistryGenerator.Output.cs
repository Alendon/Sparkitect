using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Sparkitect.Generator.DI.Pipeline;
using Sparkitect.Utilities;

namespace Sparkitect.Generator.Modding;

public partial class RegistryGenerator
{
    public static bool RenderRegistryRegistrationsUnit(RegistrationUnit unit, ModBuildSettings settings, out string code, out string fileName, string hintPrefix = "")
    {
        var suffix = unit.SourceKind == SourceKind.Provider ? "Providers" : "Resources";
        fileName = string.IsNullOrEmpty(hintPrefix)
            ? $"{unit.Model.TypeName}Registrations_{suffix}.g.cs"
            : $"{hintPrefix}_{unit.Model.TypeName}Registrations_{suffix}.g.cs";

        var ns = settings.ComputeOutputNamespace("Registrations");

        // Entrypoint projection only needs PropertyName per entry — it spells the
        // `[UnsafeAccessor]` stub name and the matching call-site identifier in
        // ProcessRegistrations. Files and RegistrationCode live on the IdProperties
        // projection where the actual storage and register-body emission happen.
        var entries = unit.Entries
            .OrderBy(e => e.Id)
            .Select(e => new { PropertyName = StringCase.ToPascalCase(e.Id) })
            .ToArray();

        var typePrefix = string.IsNullOrEmpty(hintPrefix) ? "" : hintPrefix + "_";

        // Top-level model gains ExtensionsNamespace + ModStructName so the UnsafeAccessor
        // decl can name the IDs-struct value-type as the first parameter (required for
        // static-method binding to a value-type target per CONTEXT.md specifics line 181).
        var categoryPascal = StringCase.ToPascalCase(unit.Model.Key);
        var extensionsNs = settings.ComputeOutputNamespace("IdExtensions");
        var modStructName = StringCase.ToPascalCase(settings.ModId) + categoryPascal + "IDs";

        var model = new
        {
            Namespace = ns,
            TypePrefix = typePrefix,
            RegistryName = unit.Model.TypeName,
            RegistryFullName = unit.Model.ContainingNamespace + "." + unit.Model.TypeName,
            CategoryKey = unit.Model.Key,
            SourceTag = suffix,
            ExtensionsNamespace = extensionsNs,
            ModStructName = modStructName,
            Entries = entries
        };

        return FluidHelper.TryRenderTemplate("Modding.RegistryRegistrations.Unit.liquid", model, out code);
    }

    internal static bool RenderRegistryIdContainer(RegistryModel model, ModBuildSettings settings, out string code, out string fileName)
    {
        fileName = $"{StringCase.ToPascalCase(model.Key)}ID.g.cs";
        var tpl = new
        {
            CategoryPascal = StringCase.ToPascalCase(model.Key)
        };
        return FluidHelper.TryRenderTemplate("Modding.RegistryIdContainer.Framework.liquid", tpl, out code);
    }

    public static bool RenderRegistryIdExtensionsFramework(RegistryModel model, ModBuildSettings settings, out string code, out string fileName)
    {
        var categoryPascal = StringCase.ToPascalCase(model.Key);
        var modPascal = StringCase.ToPascalCase(settings.ModId);
        var registrationsNs = settings.ComputeOutputNamespace("Registrations");
        var extensionsNs = settings.ComputeOutputNamespace();

        fileName = $"{model.TypeName}.IdFramework.g.cs";
        var tpl = new
        {
            ExtensionsNamespace = extensionsNs,
            CategoryPascal = categoryPascal,
            ModIdPascal = modPascal,
            ModStructName = modPascal + categoryPascal + "IDs",
            RegistrationsNamespace = registrationsNs,
            RegistryName = model.TypeName
        };
        return FluidHelper.TryRenderTemplate("Modding.RegistryIdExtensions.Framework.liquid", tpl, out code);
    }

    /// <summary>
    /// Renders one <c>partial class … : IHasIdentification</c> declaration per <see cref="TypeRegistrationEntry"/>
    /// in the unit. Each declaration lives in the concrete's own namespace and forwards its static
    /// <c>Identification</c> property to the matching <c>Registrations.{Registry}_{Source}.{ItemId}</c>
    /// static field already populated at boot via <c>IdentificationManager.RegisterObject(...)</c>.
    /// </summary>
    /// <remarks>
    /// Per D-04: filter is <see cref="System.Linq.Enumerable.OfType{TResult}(System.Collections.IEnumerable)"/>
    /// of <see cref="TypeRegistrationEntry"/> only — no <c>KeyedFactoryGeneration</c> filter. Auto-emit applies
    /// to ALL type-registered concretes, not only marker-flagged ones.
    /// </remarks>
    public static bool RenderAutoEmitIdentificationUnit(RegistrationUnit unit, ModBuildSettings settings,
        out string code, out string fileName)
    {
        var suffix = unit.SourceKind == SourceKind.Provider ? "Providers" : "Resources";
        fileName = $"{unit.Model.TypeName}.AutoEmitIdentification_{suffix}.g.cs";

        var typeEntries = unit.Entries
            .OfType<TypeRegistrationEntry>()
            .OrderBy(e => e.Id)
            .ToList();

        if (typeEntries.Count == 0)
        {
            code = string.Empty;
            return false;
        }

        var registrationsNs = settings.ComputeOutputNamespace("Registrations");

        var entries = typeEntries
            .Select(e =>
            {
                SplitTypeFullName(e.TypeFullName, out var ns, out var simpleName);
                return new
                {
                    TypeSimpleName = simpleName,
                    ContainingNamespace = ns,
                    TypeKindKeyword = e.TypeKind switch
                    {
                        RegistrationTypeKind.RecordStruct => "record struct",
                        RegistrationTypeKind.Record => "record",
                        RegistrationTypeKind.Struct => "struct",
                        _ => "class"
                    },
                    PropertyName = StringCase.ToPascalCase(e.Id)
                };
            })
            .ToArray();

        // Route the static Identification accessor through the IDs extension chain
        // (`IDs.{CategoryPascal}ID.{ModIdPascal}.{PropertyName}`) rather than reading
        // the entrypoint class's storage directly. ExtensionsNamespace is projected so
        // the template can emit a `using` directive that brings the C# 14
        // `extension(IDs.{Cat}ID)` chain into scope at the concrete type's namespace.
        var tpl = new
        {
            RegistrationsNamespace = registrationsNs,
            ExtensionsNamespace = settings.ComputeOutputNamespace("IdExtensions"),
            TypePrefix = string.Empty,
            RegistryName = unit.Model.TypeName,
            SourceTag = suffix,
            CategoryPascal = StringCase.ToPascalCase(unit.Model.Key),
            ModIdPascal = StringCase.ToPascalCase(settings.ModId),
            Entries = entries
        };

        return FluidHelper.TryRenderTemplate("Modding.RegistryAutoEmitIdentification.Unit.liquid", tpl, out code);
    }

    /// <summary>
    /// Splits a fully-qualified type name (e.g. <c>"global::Ns.Sub.TypeName"</c>) into its containing
    /// namespace and simple type name. The leading <c>global::</c> prefix is stripped.
    /// </summary>
    private static void SplitTypeFullName(string typeFullName, out string containingNamespace,
        out string typeSimpleName)
    {
        var name = typeFullName.StartsWith("global::") ? typeFullName.Substring("global::".Length) : typeFullName;
        var lastDot = name.LastIndexOf('.');
        if (lastDot < 0)
        {
            containingNamespace = string.Empty;
            typeSimpleName = name;
            return;
        }

        containingNamespace = name.Substring(0, lastDot);
        typeSimpleName = name.Substring(lastDot + 1);
    }

    public static bool RenderRegistryIdPropertiesUnit(RegistrationUnit unit, ModBuildSettings settings, out string code, out string fileName, string hintPrefix = "")
    {
        var suffix = unit.SourceKind == SourceKind.Provider ? "Providers" : "Resources";
        fileName = string.IsNullOrEmpty(hintPrefix)
            ? $"{unit.Model.TypeName}.IdProperties_{suffix}.g.cs"
            : $"{hintPrefix}_{unit.Model.TypeName}.IdProperties_{suffix}.g.cs";

        var categoryPascal = StringCase.ToPascalCase(unit.Model.Key);
        var registrationsNs = settings.ComputeOutputNamespace("Registrations");
        var extensionsNs = settings.ComputeOutputNamespace("IdExtensions");

        var typePrefix = string.IsNullOrEmpty(hintPrefix) ? "" : hintPrefix + "_";

        // Anonymous projection carries LowerCaseId, Files, RegistrationCode, plus
        // top-level RegistryFullName, CategoryKey, ModId — so the per-entry Register
        // method body can render fully inside the IDs struct without any forwarder.
        var entries = unit.Entries
            .OrderBy(e => e.Id)
            .Select(e =>
            {
                var propName = StringCase.ToPascalCase(e.Id);
                var lowerId = ToCamelCase(propName);
                // Backward-coordinate annotation: the template prepends `global::`, so strip any
                // leading `global::` here. Null target = no [RegisteredFrom] emitted (e.g. resource
                // entries, whose YAML coordinate branch is added in a later plan — Pitfall 1 guard).
                var registeredType = e.RegisteredTypeFullName is { Length: > 0 } rt
                    ? (rt.StartsWith("global::") ? rt.Substring("global::".Length) : rt)
                    : null;
                // YAML-backed leaves carry a PLAIN path + line/column coordinate (D-50) instead of a
                // C# typeof target. Surface it for the template's mutually-exclusive YAML branch.
                // SourcePath present (non-empty) selects the plain-coordinate form; otherwise the
                // typeof branch (or no attribute) applies. The two are never emitted together.
                var sourcePath = e is ResourceRegistrationEntry { SourcePath: { Length: > 0 } sp } ? sp : null;
                var sourceLine = e is ResourceRegistrationEntry res ? res.SourceLine : 0;
                var sourceColumn = e is ResourceRegistrationEntry res2 ? res2.SourceColumn : 0;
                return new
                {
                    Id = e.Id,
                    PropertyName = propName,
                    LowerCaseId = lowerId,
                    Files = e.Files.OrderBy(f => f.fileId).Select(f => new { fileId = f.fileId, fileName = f.fileName }).ToArray(),
                    // Pass the private backing-field name (_{lowerId}_{suffix}) so the emitted
                    // registry.RegisterX<T>(...) body inside Register_{X}_{Suffix} writes through
                    // the private static field on the IDs struct directly — NOT through the
                    // public PropertyName accessor.
                    RegistrationCode = e.EmitRegistrationEntryCode("registry", $"_{lowerId}_{suffix}"),
                    // typeof target FQN (no global:: prefix; template adds it) + optional member name.
                    RegisteredTypeFullName = registeredType,
                    RegisteredMember = e.RegisteredMember,
                    // YAML plain-coordinate fields (D-50). SourcePath is the project-relative path;
                    // SourceLine/SourceColumn are the entry-id scalar's 1-based position.
                    SourcePath = sourcePath,
                    SourceLine = sourceLine,
                    SourceColumn = sourceColumn
                };
            })
            .ToArray();

        var tpl = new
        {
            ExtensionsNamespace = extensionsNs,
            ModStructName = StringCase.ToPascalCase(settings.ModId) + categoryPascal + "IDs",
            RegistrationsNamespace = registrationsNs,
            TypePrefix = typePrefix,
            RegistryName = unit.Model.TypeName,
            RegistryFullName = unit.Model.ContainingNamespace + "." + unit.Model.TypeName,
            CategoryKey = unit.Model.Key,
            ModId = settings.ModId,
            SourceTag = suffix,
            Entries = entries
        };

        return FluidHelper.TryRenderTemplate("Modding.RegistryIdProperties.Unit.liquid", tpl, out code);
    }

    /// <summary>
    /// Lowercases the first character of a PascalCase identifier. Used to derive the
    /// <c>_{lowerCaseId}_{Suffix}</c> backing-field name from the public PropertyName.
    /// StringCase has no public ToCamelCase helper today, so this local form is kept
    /// alongside the sole caller.
    /// </summary>
    private static string ToCamelCase(string pascal)
    {
        if (string.IsNullOrEmpty(pascal)) return pascal;
        if (pascal.Length == 1) return pascal.ToLowerInvariant();
        return char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
    }
    
    internal static bool RenderRegistryMetadata(RegistryModel model, ModBuildSettings settings, out string code, out string fileName)
    {
        fileName = $"{model.TypeName}_Metadata.g.cs";
        
        var methodsMetadata = model.RegisterMethods.Select(method => new
        {
            FunctionName = method.FunctionName,
            PrimaryParameterKind = (int)method.PrimaryParameterKind,
            Constraint = (int)method.Constraint,
            TypeConstraint = string.Join(";", method.TypeConstraint),
            KeyedFactoryMarkerTBase = method.KeyedFactoryMarkerTBase ?? string.Empty,
            KeyedFactoryMarkerTKey = method.KeyedFactoryMarkerTKey ?? string.Empty
        }).ToArray();
        
        var registerMethodsString = string.Join(";", model.RegisterMethods.Select(m => m.FunctionName));
        
        var resourceFilesString = string.Join(";", model.ResourceFiles.Select(rf =>
            $"{rf.Key}:{(rf.Required ? 1 : 0)}:{(rf.Primary ? 1 : 0)}"));
        
        var metadataModel = new
        {
            Namespace = settings.ComputeOutputNamespace(),
            MetadataClassName = $"{model.TypeName}_Metadata",
            TypeName = model.TypeName,
            Key = model.Key,
            ContainingNamespace = model.ContainingNamespace,
            IsExternal = model.IsExternal ? "true" : "false",
            RegisterMethods = registerMethodsString,
            ResourceFiles = resourceFilesString,
            RegisterMethodsMetadata = methodsMetadata
        };
        
        return FluidHelper.TryRenderTemplate("Modding.RegistryMetadata.liquid", metadataModel, out code);
    }

    internal static bool RenderRegistryConfigurator(
        ImmutableArray<RegistryWithFactory> registriesWithFactories,
        ModBuildSettings settings,
        out string configuratorCode,
        out string configuratorFileName,
        out string shellCode,
        out string shellFileName)
    {
        shellFileName = "RegistryConfigurator_Shell.g.cs";
        shellCode = string.Empty;

        if (registriesWithFactories.IsEmpty)
        {
            configuratorCode = string.Empty;
            configuratorFileName = string.Empty;
            return false;
        }

        // Extract registrations from RegistryWithFactory wrappers
        var registrations = registriesWithFactories
            .Select(r => r.FactoryData.Registration)
            .ToImmutableValueArray();

        var options = new ConfiguratorOptions(
            ClassName: "RegistryConfigurator",
            Namespace: settings.ComputeOutputNamespace(),
            BaseType: "Sparkitect.DI.IRegistryConfigurator",
            EntrypointAttribute: "Sparkitect.DI.RegistryConfiguratorAttribute",
            Kind: new ConfiguratorKind.Keyed("string", "Sparkitect.Modding.IRegistryBase"),
            IsPartial: true,
            MethodName: "RegisterRegistries");

        // DI pipeline renders the registration method (partial class)
        var success = DiPipeline.RenderConfigurator(registrations, options,
            out configuratorCode, out configuratorFileName);

        // RegistryGenerator renders the partial class shell
        shellCode = GenerateConfiguratorShell(settings);

        return success;
    }

    private static string GenerateConfiguratorShell(ModBuildSettings settings)
    {
        return $@"#pragma warning disable CS9113
#pragma warning disable CS1591

namespace {settings.ComputeOutputNamespace()};

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::Sparkitect.DI.RegistryConfigurator]
internal partial class RegistryConfigurator : global::Sparkitect.DI.IRegistryConfigurator
{{
    public void Configure(
        global::System.Collections.Generic.IDictionary<string, global::Sparkitect.DI.IKeyedFactory<global::Sparkitect.Modding.IRegistryBase>> registrations,
        global::System.Collections.Generic.IReadOnlySet<string> loadedMods)
    {{
        RegisterRegistries(registrations, loadedMods);
    }}
}}";
    }


    internal static void OutputRegistryAttributes(SourceProductionContext context, RegistryModel model)
    {
        if (model.IsExternal) return;

        if (RenderRegistryAttributes(model, out var code, out var fileName))
        {
            context.AddSource(fileName, code);
        }
    }

    internal static void OutputRegistryMetadata(SourceProductionContext context, (RegistryModel model, ModBuildSettings setting) arg2)
    {
        var (model, settings) = arg2;
        
        if (RenderRegistryMetadata(model, settings, out var code, out var fileName))
        {
            context.AddSource(fileName, code);
        }
    }

    internal static void OutputRegistryFactory(SourceProductionContext context, (RegistryWithFactory Left, ModBuildSettings Right) pair)
    {
        var (rwf, settings) = pair;

        if (DiPipeline.RenderFactory(rwf.FactoryData.Factory, out var code, out var fileName))
        {
            context.AddSource(fileName, code);
        }

        // Emit metadata entrypoint if facade metadata was extracted
        if (rwf.FacadeMetadata.Count > 0)
        {
            var factory = rwf.FactoryData.Factory;
            var wrapperTypeName = $"{factory.ImplementationNamespace}.{factory.ImplementationTypeName}_KeyedFactory";

            var models = rwf.FacadeMetadata.Cast<IMetadataModel>().ToList();
            if (DiPipeline.RenderMetadataEntrypoint(wrapperTypeName, factory.ImplementationNamespace, models, settings,
                    out var metaCode, out var metaFileName))
                context.AddSource(metaFileName, metaCode);
        }
    }

    internal static void OutputRegistryConfigurator(SourceProductionContext context, (ImmutableArray<RegistryWithFactory> Left, ModBuildSettings Right) arg2)
    {
        var (registriesWithFactories, settings) = arg2;

        if (RenderRegistryConfigurator(registriesWithFactories, settings,
                out var configuratorCode, out var configuratorFileName,
                out var shellCode, out var shellFileName))
        {
            context.AddSource(configuratorFileName, configuratorCode);
            context.AddSource(shellFileName, shellCode);
        }
    }

    internal static void OutputRegistryIdFramework(SourceProductionContext context, (RegistryModel model, ModBuildSettings settings) arg)
    {
        if (RenderRegistryIdContainer(arg.model, arg.settings, out var code1, out var file1))
            context.AddSource(file1, code1);
        if (RenderRegistryIdExtensionsFramework(arg.model, arg.settings, out var code2, out var file2))
            context.AddSource(file2, code2);
    }

    internal static void OutputRegistryIdExtensions(SourceProductionContext context, (RegistryModel model, ModBuildSettings settings) arg)
    {
        if (RenderRegistryIdExtensionsFramework(arg.model, arg.settings, out var code, out var file))
            context.AddSource(file, code);
    }

    internal static void OutputRegistrationsUnit(SourceProductionContext context, (RegistrationUnit unit, ModBuildSettings settings) arg)
    {
        if (RenderRegistryRegistrationsUnit(arg.unit, arg.settings, out var code, out var file))
        {
            context.AddSource(file, code);
        }
    }

    // ── Branch A (per declaring assembly) — Shell + attribute + C# 14 extension accessors ──

    /// <summary>
    /// Per declaring registry assembly: for each marker-flagged method on the registry,
    /// emits the configurator shell + matching attribute (both <c>public sealed</c>) plus a
    /// single C# 14 <c>extension(TRegistry)</c> accessor container that exposes one Type getter
    /// and one builder helper per marker-flagged method.
    /// </summary>
    /// <remarks>
    /// Re-rooted onto <see cref="RegistryWithFactory"/> (the same provider <see cref="OutputRegistryConfigurator"/>
    /// uses) so the attribute Type exists in the declaring assembly — consumers can <c>typeof()</c> it,
    /// and the runtime no longer needs simple-name reflection to discover it.
    /// </remarks>
    internal static void OutputKeyedFactoryShellAndAccessors(
        SourceProductionContext context,
        (RegistryWithFactory Left, ModBuildSettings Right) pair)
    {
        var (rwf, settings) = pair;
        var registry = rwf.Registry;

        var markerMethods = registry.RegisterMethods
            .Where(m => !string.IsNullOrEmpty(m.KeyedFactoryMarkerTBase))
            .ToList();
        if (markerMethods.Count == 0) return;

        var compilerGeneratedNs = settings.ComputeOutputNamespace();

        // (1) Per marker-flagged method: emit the shell+attribute file.
        foreach (var method in markerMethods)
        {
            var configuratorClassName = $"{registry.TypeName}_{method.FunctionName}_KeyedFactoryConfigurator";
            var shellFileName = $"{configuratorClassName}_Shell.g.cs";
            var shellCode = GenerateKeyedFactoryConfiguratorShell(
                compilerGeneratedNs, configuratorClassName);
            context.AddSource(shellFileName, shellCode);
        }

        // (2) One extension-container file per registry, holding all marker-method accessors.
        var extensionsFileName = $"{registry.TypeName}_KeyedFactoryExtensions.g.cs";
        var extensionsCode = GenerateKeyedFactoryExtensions(
            compilerGeneratedNs, registry, markerMethods);
        context.AddSource(extensionsFileName, extensionsCode);
    }

    /// <summary>
    /// Per consumer assembly: for every marker-flagged <see cref="TypeRegistrationEntry"/> in the unit,
    /// emits a non-partial <c>internal sealed class</c> implementing
    /// <c>IFactoryConfiguratorBase&lt;TKey,TBase&gt;</c> and carrying the (now-public) configurator
    /// attribute from the declaring assembly. Replaces the pre-260511-lio per-consumer
    /// configurator-partial-body emission. C# partials are file-local, so consumers can no longer
    /// "extend" the declaring-assembly shell — they emit a fresh class instead.
    /// </summary>
    internal static void OutputKeyedFactoryRegistrations(
        SourceProductionContext context,
        (RegistrationUnit unit, ModBuildSettings settings) arg)
    {
        foreach (var group in RenderKeyedFactoryRegistrations(arg.unit, arg.settings))
        {
            context.AddSource(group.FileName, group.Code);
        }
    }

    public sealed record KeyedFactoryRegistrationsEmission(string FileName, string Code);

    /// <summary>
    /// For each marker-flagged method group inside the consuming unit, renders one
    /// <c>{ModNs}_{Registry}_{Method}_KeyedFactoryRegistrations</c> class.
    /// </summary>
    public static ImmutableArray<KeyedFactoryRegistrationsEmission> RenderKeyedFactoryRegistrations(
        RegistrationUnit unit, ModBuildSettings settings)
    {
        var result = ImmutableArray.CreateBuilder<KeyedFactoryRegistrationsEmission>();

        var markedEntries = unit.Entries
            .OfType<TypeRegistrationEntry>()
            .Where(e => e.KeyedFactoryGeneration is not null)
            .ToList();

        if (markedEntries.Count == 0)
            return result.ToImmutable();

        // Attribute lives in the registry's *declaring* SG namespace (which equals the consumer's
        // when the registry is in the same compilation, and the referenced assembly's metadata
        // class's containing namespace otherwise).
        var consumerSgNs = settings.ComputeOutputNamespace();
        var attrHostNs = !string.IsNullOrEmpty(unit.Model.DeclaringSgNamespace)
            ? unit.Model.DeclaringSgNamespace!
            : consumerSgNs;
        var modIdPascal = StringCase.ToPascalCase(settings.ModId);

        // Group by method name — one registrations class per (registry × method) pair within this consumer.
        var grouped = markedEntries
            .GroupBy(e => e.MethodName)
            .ToList();

        foreach (var group in grouped)
        {
            var firstEntry = group.First();
            // ConfiguratorClassName carries the {Registry}_{Method}_KeyedFactoryConfigurator stem;
            // pull it apart to recover the registry+method names without re-deriving from KeyedFactoryGenerationInfo.
            var configuratorClassName = firstEntry.KeyedFactoryGeneration!.ConfiguratorClassName;
            var tBaseFullName = firstEntry.KeyedFactoryGeneration!.TBaseFullName;
            var tBaseWithoutGlobal = tBaseFullName.StartsWith("global::")
                ? tBaseFullName.Substring("global::".Length)
                : tBaseFullName;
            var tBaseWithGlobal = tBaseFullName.StartsWith("global::") ? tBaseFullName : $"global::{tBaseFullName}";

            // Registrations-class name — intra-assembly-unique per the CONTEXT.md naming convention.
            var className = $"{modIdPascal}_{configuratorClassName}_Registrations";
            var fileName = $"{className}.g.cs";

            // Body: one `registrations[id] = new {Concrete}_KeyedFactory();` per entry.
            var lines = new System.Text.StringBuilder();
            foreach (var entry in group)
            {
                var typeFullName = entry.TypeFullName;
                var factoryTypeName = $"{typeFullName}_KeyedFactory";
                lines.AppendLine(
                    $"        registrations[global::Sparkitect.Modding.IdentificationHelper.Read<{typeFullName}>()] = new {factoryTypeName}();");
            }

            var attrFullName = $"global::{attrHostNs}.{configuratorClassName}Attribute";

            var code =
$@"#pragma warning disable CS9113
#pragma warning disable CS1591

namespace {consumerSgNs};

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[{attrFullName}]
internal sealed class {className}
    : global::Sparkitect.DI.IFactoryConfiguratorBase<global::Sparkitect.Modding.Identification, {tBaseWithGlobal}>
{{
    public void Configure(
        global::System.Collections.Generic.IDictionary<global::Sparkitect.Modding.Identification, global::Sparkitect.DI.IKeyedFactory<{tBaseWithGlobal}>> registrations,
        global::System.Collections.Generic.IReadOnlySet<string> loadedMods)
    {{
{lines.ToString().TrimEnd('\r', '\n')}
    }}
}}";

            result.Add(new KeyedFactoryRegistrationsEmission(fileName, code));
        }

        return result.ToImmutable();
    }

    /// <summary>
    /// Renders the per-method shell file holding the public configurator attribute and a
    /// public marker class. The shell no longer carries a <c>Configure</c> body — per-consumer
    /// registrations classes implement <c>IFactoryConfiguratorBase</c> directly and
    /// <c>BuildFactoryContainer</c> aggregates them.
    /// </summary>
    internal static string GenerateKeyedFactoryConfiguratorShell(
        string namespaceName, string configuratorClassName)
    {
        var attrName = $"{configuratorClassName}Attribute";
        return $@"#pragma warning disable CS9113
#pragma warning disable CS1591

namespace {namespaceName};

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
[global::System.AttributeUsage(global::System.AttributeTargets.Class, Inherited = false)]
public sealed class {attrName} : global::System.Attribute {{ }}

[global::System.Runtime.CompilerServices.CompilerGeneratedAttribute]
public sealed class {configuratorClassName} {{ }}";
    }

    /// <summary>
    /// Renders the C# 14 <c>extension(TRegistry)</c> accessor container — one file per registry,
    /// one static Type getter + one static builder helper per marker-flagged method.
    /// Lives in the <c>{SgOutputNamespace}.KeyedFactoryExtensions</c> sub-namespace, mirroring
    /// the <c>IdExtensions</c> precedent at <see cref="RenderRegistryIdExtensionsFramework"/>.
    /// </summary>
    internal static string GenerateKeyedFactoryExtensions(
        string compilerGeneratedNs, RegistryModel registry,
        System.Collections.Generic.IReadOnlyList<RegisterMethodModel> markerMethods)
    {
        var extensionsNs = $"{compilerGeneratedNs}.KeyedFactoryExtensions";
        var containerClassName = $"{registry.TypeName}KeyedFactoryExtensions";
        var registryFqn = $"global::{registry.ContainingNamespace}.{registry.TypeName}";

        var members = new System.Text.StringBuilder();
        for (int i = 0; i < markerMethods.Count; i++)
        {
            var method = markerMethods[i];
            var configuratorClassName = $"{registry.TypeName}_{method.FunctionName}_KeyedFactoryConfigurator";
            var attrFqn = $"global::{compilerGeneratedNs}.{configuratorClassName}Attribute";

            var tBase = method.KeyedFactoryMarkerTBase!;
            var tBaseFqn = tBase.StartsWith("global::") ? tBase : $"global::{tBase}";
            var tKey = method.KeyedFactoryMarkerTKey ?? "Sparkitect.Modding.Identification";
            var tKeyFqn = tKey.StartsWith("global::") ? tKey : $"global::{tKey}";

            if (i > 0) members.AppendLine();
            members.AppendLine(
$@"        public static global::System.Type {method.FunctionName}ConfiguratorAttribute
            => typeof({attrFqn});

        public static global::Sparkitect.DI.Container.IFactoryContainer<{tKeyFqn}, {tBaseFqn}> Build{method.FunctionName}Container(
            global::Sparkitect.DI.IDIService di,
            global::Sparkitect.DI.Container.ICoreContainer container,
            global::Sparkitect.DI.Resolution.IResolutionProvider? provider,
            global::System.Collections.Generic.IEnumerable<string> modIds)
            => di.BuildFactoryContainer<{tKeyFqn}, {tBaseFqn}>(
                container,
                provider,
                modIds,
                typeof({attrFqn}));");
        }

        return $@"#nullable enable
#pragma warning disable CS9113
#pragma warning disable CS1591

namespace {extensionsNs};

public static class {containerClassName}
{{
    extension({registryFqn})
    {{
{members.ToString().TrimEnd('\r', '\n')}
    }}
}}";
    }

    /// <summary>
    /// Branch B callback: emits one _KeyedFactory.g.cs per marker-flagged concrete type.
    /// The MarkerProviderConcrete carries the pre-computed FactoryModel (symbol-free, incremental-cacheable).
    /// </summary>
    internal static void OutputMarkerKeyedFactoryClass(
        SourceProductionContext context,
        (MarkerProviderConcrete concrete, ModBuildSettings settings) arg)
    {
        if (DiPipeline.RenderFactory(arg.concrete.Factory, out var code, out var fileName))
            context.AddSource(fileName, code);
    }

    internal static void OutputIdPropertiesUnit(SourceProductionContext context, (RegistrationUnit unit, ModBuildSettings settings) arg)
    {
        if (RenderRegistryIdPropertiesUnit(arg.unit, arg.settings, out var code, out var file))
        {
            context.AddSource(file, code);
        }
    }

    /// <summary>
    /// Output handler that emits one auto-emitted <c>IHasIdentification</c> partial-class file per
    /// <see cref="RegistrationUnit"/>. No-op when the unit contains no <see cref="TypeRegistrationEntry"/>
    /// (e.g. value/method/property providers).
    /// </summary>
    internal static void OutputAutoEmitIdentificationUnit(SourceProductionContext context, (RegistrationUnit unit, ModBuildSettings settings) arg)
    {
        if (RenderAutoEmitIdentificationUnit(arg.unit, arg.settings, out var code, out var file))
        {
            context.AddSource(file, code);
        }
    }
}
