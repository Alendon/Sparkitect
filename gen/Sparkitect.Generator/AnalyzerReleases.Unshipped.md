## Release 1.1

### New Rules

<!-- Category 01: DI/Factory Diagnostics -->

| Rule ID   | Category   | Severity | Notes                                                                 |
|-----------|------------|----------|-----------------------------------------------------------------------|
| SPARK0101 | Sparkitect | Warning  | DI Only abstract/interface dependencies                               |
| SPARK0102 | Sparkitect | Error    | DI Only one constructor                                               |
| SPARK0103 | Sparkitect | Warning  | DI Required properties should be init-only                            |
| SPARK0104 | Sparkitect | Warning  | DI Single generation marker                                           |
| SPARK0105 | Sparkitect | Error    | DI Conflicting generation markers                                     |
| SPARK0106 | Sparkitect | Error    | DI KeyedFactory missing key association                               |
| SPARK0107 | Sparkitect | Error    | DI Invalid key property                                               |
| SPARK0108 | Sparkitect | Error    | DI Conflicting key associations                                       |

<!-- Category 02: Registry/Modding Diagnostics -->

| SPARK0201 | Sparkitect | Error    | Registry requires IRegistry                                           |
| SPARK0202 | Sparkitect | Error    | Registry missing Identifier                                           |
| SPARK0203 | Sparkitect | Error    | Registry must be top-level in a namespace                             |
| SPARK0204 | Sparkitect | Error    | Duplicate category key                                                |
| SPARK0205 | Sparkitect | Warning  | Duplicate registry type names across namespaces                       |
| SPARK0206 | Sparkitect | Error    | Category Identifier must be snake_case (letters, digits, underscores) |
| SPARK0210 | Sparkitect | Error    | [RegistryMethod] outside a registry                                   |
| SPARK0211 | Sparkitect | Error    | Invalid registry method signature                                     |
| SPARK0212 | Sparkitect | Error    | Too many type parameters                                              |
| SPARK0213 | Sparkitect | Error    | Generic value method mismatch                                         |
| SPARK0214 | Sparkitect | Error    | First parameter must be Identification                                |
| SPARK0215 | Sparkitect | Error    | Duplicate registry method names                                       |
| SPARK0216 | Sparkitect | Error    | UseResourceFile missing Key                                           |
| SPARK0217 | Sparkitect | Error    | Duplicate resource file key                                           |
| SPARK0218 | Sparkitect | Error    | Multiple primary resource files                                       |
| SPARK0220 | Sparkitect | Error    | Provider missing id                                                   |
| SPARK0221 | Sparkitect | Error    | Provider member must be static                                        |
| SPARK0222 | Sparkitect | Error    | Unknown registry reference                                            |
| SPARK0223 | Sparkitect | Error    | Unknown registry method                                               |
| SPARK0224 | Sparkitect | Error    | Provider kind mismatch                                                |
| SPARK0225 | Sparkitect | Error    | Provider return type incompatible                                     |
| SPARK0226 | Sparkitect | Error    | Type does not satisfy generic constraints                             |
| SPARK0230 | Sparkitect | Error    | Duplicate registration id within a registry                           |
| SPARK0231 | Sparkitect | Error    | Registration id must be snake_case (letters, digits, underscores)     |
| SPARK0232 | Sparkitect | Warning  | DI parameter guidance (prefer abstract/interface or nullable)         |
| SPARK0242 | Sparkitect | Error    | YAML unknown registry/method in key                                   |
| SPARK0243 | Sparkitect | Warning  | YAML unknown file key                                                 |
| SPARK0244 | Sparkitect | Error    | YAML missing required file key                                        |
| SPARK0245 | Sparkitect | Error    | YAML duplicate id per registry                                        |
| SPARK0250 | Sparkitect | Error    | Duplicate normalized property name collision                          |

<!-- Category 03: GameState/StateService Diagnostics -->

| SPARK0301 | Sparkitect | Error    | StateService does not implement declared interface                    |
| SPARK0302 | Sparkitect | Error    | StateService does not implement required facade                       |
| SPARK0303 | Sparkitect | Error    | StateService interface missing StateFacade attribute                  |

<!-- Category 04: Stateless Function Diagnostics -->

| SPARK0401 | Sparkitect | Error    | Stateless function must be static                                     |
| SPARK0402 | Sparkitect | Error    | Multiple scheduling attributes not allowed                            |
| SPARK0403 | Sparkitect | Warning  | Parameter may not be DI-resolvable                                    |
| SPARK0404 | Sparkitect | Error    | Container must implement IHasIdentification                           |
| SPARK0405 | Sparkitect | Warning  | Ordering attribute without scheduling                                 |
