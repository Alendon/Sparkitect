# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Sparkitect is a modular game engine framework with a focus on extensibility through mods. The project is written in C# (.NET 9.0) and uses a component-based architecture.

## Building and Running

### Build Commands

```bash
# Build the entire solution
dotnet build

# Build a specific project
dotnet build src/Sparkitect/Sparkitect.csproj
dotnet build src/Sparkitect.Sdk/Sparkitect.Sdk.csproj

# Build in release mode
dotnet build -c Release
```

### Testing

```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/Sparkitect.Tests/Sparkitect.Tests.csproj
dotnet test tests/Sparkitect.Generator.Tests/Sparkitect.Generator.Tests.csproj

# Run a specific test (using TUnit)
dotnet test tests/Sparkitect.Generator.Tests/Sparkitect.Generator.Tests.csproj --filter "SingleLogStatement"
```

### Packaging

```bash
# Create NuGet packages
dotnet pack src/Sparkitect/Sparkitect.csproj
dotnet pack src/Sparkitect.Sdk/Sparkitect.Sdk.csproj

# Create mod archive
# This happens automatically during build for mod projects
dotnet build samples/MinimalSampleMod/MinimalSampleMod.csproj
```

## Architecture Overview

### Core Components

1. **Custom Dependency Injection System**
   - Implementing a custom DI framework to replace external dependencies
   - Service registration through attribute-based discovery
   - Three container types: Core, Configuration, and Factory containers

2. **Entity Component System (ECS)**
   - Component-based architecture for game objects
   - Systems for logic/behavior
   - Entity management with stable/volatile IDs

3. **Modding System**
   - Load mods from .sparkmod archives
   - Registry pattern for mod content/features
   - Identification system for mod resources

4. **Game State Management**
   - State-based game flow control
   - Attribute-driven state data

5. **Vulkan Graphics Integration**
   - Abstraction over Vulkan API using Silk.NET

### Project Structure

- `src/Sparkitect/` - Core engine
- `src/Sparkitect.Sdk/` - SDK for mod development (MSBuild integration)
- `gen/Sparkitect.Generator/` - Source generators
- `samples/MinimalSampleMod/` - Example mod
- `tests/` - Test projects
- `benchmark/` - Performance benchmarks

## SDK and Mod Development

The Sparkitect.Sdk provides MSBuild integration for mod development:

1. Mod projects should import the Sparkitect.Sdk.props and .targets files
2. Required properties:
   - ModName
   - ModIdentifier
   - ModVersion
   - ModAuthor
   - ModDescription
   - ModType (Root or Game)

During build, the SDK:
1. Validates mod properties
2. Generates a mod manifest JSON file
3. Creates a .sparkmod archive containing the mod assembly and dependencies
4. Generates a development launcher

## Development Workflow

1. Make changes to core engine or SDK
2. Run tests to verify functionality
3. Build sample mod to test integration
4. If needed, update version numbers in csproj files

## Coding Style and Architecture Design

### Code Style

1. **Clean Architecture Principles**
   - Clear separation of concerns
   - Interface-based design for testability
   - Dependency inversion
   - Explicit dependencies in constructors

2. **Naming Conventions**
   - PascalCase for class names, properties, and methods
   - camelCase for local variables and parameters
   - Interfaces prefixed with 'I'
   - Descriptive, self-documenting names

3. **Nullable Reference Types**
   - Enabled throughout the codebase
   - Strict null checking with `WarningsAsErrors: Nullable` - compilation fails on null safety violations
   - Use pattern matching for null checks (e.g., `if (obj is null)`)
   - Add appropriate null validation checks at API boundaries
   - Use `[NotNullWhen(true)]` and other null annotations for better compiler analysis
   - Prefer `as`/`is` operators over direct casting for type safety

4. **Modern C# Features**
   - Use the new collection syntax (e.g., `Type[] array = [];` instead of `Array.Empty<Type>()`)
   - Prefer collection expressions (e.g., `[1, 2, 3]`) over traditional initialization
   - Use pattern matching where appropriate
   - Take advantage of C# 9+ features (records, init-only properties, etc.)
   - Prefer explicit over implicit

5. **Code Clarity**
   - Self-documenting code with minimal comments
   - Comments only for explaining "why", not "what" or "how"
   - Use meaningful variable names
   - Keep methods focused and reasonably sized

6. **Immutability**
   - Prefer immutability where possible
   - Use init-only properties for configuration objects
   - Readonly collections where appropriate

7. **Error Handling**
   - Structured logging with Serilog
   - Early validation and precise exceptions
   - Proper context in error messages

### Architecture Style

1. **Modular Design**
   - Each feature in its own namespace
   - Loose coupling between modules
   - Communication through well-defined interfaces

2. **Dependency Injection**
   - Constructor injection as the primary pattern
   - Factory-based service instantiation for complex scenarios
   - Custom DI framework with attribute-based registration
   - Container hierarchy: Core -> Factory -> Configuration

3. **Registry Pattern**
   - Central registration of game elements
   - Phased initialization
   - Identifier-based resource management

4. **Component-Based Design**
   - Entity Component System for gameplay elements
   - Composition over inheritance
   - Data-oriented where performance is critical

5. **Mod System Design**
   - Clean separation between mods
   - Versioned dependencies
   - Resource isolation

## Custom DI Framework Implementation

The current goal is to replace external DI with a custom framework built into Sparkitect. Key elements include:

1. **Container Hierarchy**
   - `ICoreContainer`: Basic services and engine components
   - `IFactoryContainer`: Service factories and complex construction logic
   - `IConfigurationContainer`: Configuration and mod-specific services

2. **Attribute-Based Registration**
   - `[CoreContainerConfiguratorEntrypoint]`: Entry point for core container configuration
   - `[ServiceFactory]`: Marks classes as service factories
   - `[SingletonAttribute]`: Marks a class to be registered as a singleton

3. **Service Factory Pattern**
   - Provides type-safe dependency registration and resolution
   - Supports optional dependencies and circular property dependencies

## Documentation References

Always refer to the following documentation:

1. **API Documentation**
   - Review the `/docs/api/` directory for detailed API documentation

2. **Guides in Articles**
   - Consult `/docs/articles/` for architectural guidance

3. **Core Interfaces**
   - `IContainer` and derived interfaces
   - ECS interfaces (`IComponent`, `IEntityManager`, etc.)
   - Modding interfaces (`IModManager`, `IRegistry`)

4. **Generator Functionality**
   - `LogEnricherGenerator` and other source generators
   - Attribute handlers and code generation templates

## Key Technologies

- .NET 9.0
- Custom DI Framework (replacing external dependencies)
- Silk.NET.Vulkan (Graphics)
- Serilog (Logging)
- TUnit (Testing)
- Source Generators (CodeGen)
