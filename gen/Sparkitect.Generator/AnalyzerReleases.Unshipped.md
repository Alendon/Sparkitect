; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
SPARK0201 | Sparkitect | Error | [Registry] requires IRegistry<TModule>
SPARK0202 | Sparkitect | Error | Registry must specify Identifier
SPARK0203 | Sparkitect | Error | Registry must be top-level in a namespace
SPARK0204 | Sparkitect | Error | Duplicate registry category Identifier
SPARK0205 | Sparkitect | Warning | Duplicate registry type names across namespaces
SPARK0206 | Sparkitect | Error | Category Identifier must be snake_case
SPARK0210 | Sparkitect | Error | [RegistryMethod] only inside registries
SPARK0211 | Sparkitect | Error | Invalid [RegistryMethod] signature
SPARK0213 | Sparkitect | Error | Generic value method mismatch
SPARK0214 | Sparkitect | Error | First parameter must be Identification
SPARK0215 | Sparkitect | Error | Duplicate registry method name
SPARK0216 | Sparkitect | Error | UseResourceFile missing Key
SPARK0217 | Sparkitect | Error | Duplicate resource file key
SPARK0218 | Sparkitect | Error | Multiple primary resource files
SPARK0220 | Sparkitect | Error | Provider attribute requires id
SPARK0221 | Sparkitect | Error | Provider member must be static
SPARK0222 | Sparkitect | Error | Referenced registry not discoverable
SPARK0223 | Sparkitect | Error | Unknown registry method
SPARK0224 | Sparkitect | Error | Provider kind does not match registry method
SPARK0225 | Sparkitect | Error | Provider return type incompatible
SPARK0226 | Sparkitect | Error | Type does not satisfy generic constraints
SPARK0230 | Sparkitect | Error | Duplicate registration id
SPARK0232 | Sparkitect | Warning | Prefer abstract/interface DI parameters
SPARK0242 | Sparkitect | Error | Unknown registry/method in YAML key
SPARK0243 | Sparkitect | Warning | Unknown file key in YAML
SPARK0244 | Sparkitect | Error | Missing required file key in YAML
SPARK0245 | Sparkitect | Error | Duplicate id in YAML
SPARK0250 | Sparkitect | Error | Duplicate property name after normalization
SPARK0260 | Sparkitect | Error | [KeyedFactoryGenerationMarker] only on type-registration registry methods
SPARK0261 | Sparkitect | Error | Marker-flagged registry method missing required constraints
SPARK0262 | Sparkitect | Warning | Hand-authored ': IHasIdentification' without a registration attribute
SPARK0263 | Sparkitect | Warning | Registered concrete missing explicit ': IHasIdentification'
SPARK0270 | Sparkitect | Error | Duplicate settings group ownership
SPARK0271 | Sparkitect | Error | At most one bare [TypedIdentification] marker per register method
SPARK0272 | Sparkitect | Error | Registry has an incoherent typed-identification shape
SPARK0273 | Sparkitect | Error | Invalid [TypedIdentification<TTargetRegistry>] target
SPARK0274 | Sparkitect | Error | Typed-identification alias name collides with a real member
SPARK0301 | Sparkitect | Error | StateService does not implement declared interface
SPARK0302 | Sparkitect | Error | StateService does not implement required facade
SPARK0304 | Sparkitect | Error | Facade interface missing [FacadeFor] attribute
SPARK0305 | Sparkitect | Error | FacadeFor attribute inconsistent with service facade declaration
SPARK0306 | Sparkitect | Error | Registered state/module type mis-shaped for composition contract
SPARK0401 | Sparkitect | Error | Stateless function must be static
SPARK0402 | Sparkitect | Error | Multiple scheduling attributes not allowed
SPARK0403 | Sparkitect | Warning | Parameter may not be DI-resolvable
SPARK0404 | Sparkitect | Error | Container must implement IHasIdentification
SPARK0406 | Sparkitect | Error | Stateless function must not access non-public static state
SPARK0501 | Sparkitect | Error | ModId must be snake_case
SPARK0502 | Sparkitect | Error | Identifier must be snake_case
SPARK0601 | Sparkitect | Error | Optional mod type leakage
SPARK0602 | Sparkitect | Error | Unguarded optional mod call
SPARK0603 | Sparkitect | Error | Invalid optional mod ID
SPARK0701 | Sparkitect | Warning | Metadata parameter attribute is not harvested here
