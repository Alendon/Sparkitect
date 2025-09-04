## Release 0.1.0 (Unshipped)

### New Rules

| Rule ID   | Category   | Severity | Notes                                                                 |
|-----------|------------|----------|-----------------------------------------------------------------------|
| SPARK1001 | Sparkitect | Warning  | DI Only abstract/interface dependencies                               |
| SPARK1002 | Sparkitect | Error    | DI Only one constructor                                               |
| SPARK1003 | Sparkitect | Warning  | DI Required properties should be init-only                            |
| SPARK1004 | Sparkitect | Warning  | DI Single generation marker                                           |
| SPARK1005 | Sparkitect | Error    | DI Conflicting generation markers                                     |
| SPARK1006 | Sparkitect | Error    | DI KeyedFactory missing key association                               |
| SPARK1007 | Sparkitect | Error    | DI Invalid key property                                               |
| SPARK1008 | Sparkitect | Error    | DI Conflicting key associations                                       |
| SPARK2001 | Sparkitect | Error    | Registry requires IRegistry                                           |
| SPARK2002 | Sparkitect | Error    | Registry missing Identifier                                           |
| SPARK2003 | Sparkitect | Error    | Registry must be top-level in a namespace                             |
| SPARK2004 | Sparkitect | Error    | Duplicate category key                                                |
| SPARK2005 | Sparkitect | Warning  | Duplicate registry type names across namespaces                       |
| SPARK2006 | Sparkitect | Error    | Category Identifier must be snake_case (letters, digits, underscores) |
| SPARK2010 | Sparkitect | Error    | [RegistryMethod] outside a registry                                   |
| SPARK2011 | Sparkitect | Error    | Invalid registry method signature                                     |
| SPARK2012 | Sparkitect | Error    | Too many type parameters                                              |
| SPARK2013 | Sparkitect | Error    | Generic value method mismatch                                         |
| SPARK2014 | Sparkitect | Error    | First parameter must be Identification                                |
| SPARK2015 | Sparkitect | Error    | Duplicate registry method names                                       |
| SPARK2016 | Sparkitect | Error    | UseResourceFile missing Identifier                                    |
| SPARK2017 | Sparkitect | Error    | Duplicate resource file identifier                                    |
| SPARK2020 | Sparkitect | Error    | Provider missing id                                                   |
| SPARK2021 | Sparkitect | Error    | Provider member must be static                                        |
| SPARK2022 | Sparkitect | Error    | Unknown registry reference                                            |
| SPARK2023 | Sparkitect | Error    | Unknown registry method                                               |
| SPARK2024 | Sparkitect | Error    | Provider kind mismatch                                                |
| SPARK2025 | Sparkitect | Error    | Provider return type incompatible                                     |
| SPARK2026 | Sparkitect | Error    | Type does not satisfy generic constraints                             |
| SPARK2030 | Sparkitect | Error    | Duplicate registration id within a registry                           |
| SPARK2031 | Sparkitect | Error    | Registration id must be snake_case (letters, digits, underscores)     |
| SPARK2032 | Sparkitect | Warning  | DI parameter guidance (prefer abstract/interface or nullable)         |
| SPARK2050 | Sparkitect | Error    | Duplicate normalized property name collision                          |
| SPARK2040 | Sparkitect | Error    | YAML entry missing id                                                 |
| SPARK2041 | Sparkitect | Error    | YAML file XOR files                                                   |
| SPARK2042 | Sparkitect | Error    | YAML unknown registry/method in key                                   |
| SPARK2043 | Sparkitect | Warning  | YAML unknown file key                                                 |
| SPARK2044 | Sparkitect | Error    | YAML missing required file key                                        |
| SPARK2045 | Sparkitect | Error    | YAML duplicate id per registry                                        |

