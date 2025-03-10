# Sparkitect Development Guidelines

## Commands
- **Build:** `dotnet build Sparkitect.sln`
- **Run tests:** `dotnet test`
- **Run single test:** `dotnet test --filter FullyQualifiedName=Namespace.TestClass.TestMethod`
- **Benchmarks:** `dotnet run -c Release -p benchmark/Sparkitect.Benchmark/Sparkitect.Benchmark.csproj`
- **Documentation:** `docfx docs/docfx.json`

## Code Style
- **Formatting:** 4-space indentation, braces on new lines
- **Naming:** PascalCase for types/public members, camelCase for parameters/locals
- **Types:** Use nullable reference types, prefer readonly structs for data
- **Error handling:** Return Result<T> pattern, avoid exceptions for control flow
- **Organization:** File-scoped namespaces, one type per file

## Development Process
- **Documentation:** Always consult docs for existing feature descriptions. When adding/modifying features, adjust documentation accordingly. Always ask for doc edit confirmation.
- **Testing:** If code is testable, write tests using TUnit. Align style with existing tests.
- **Architecture:** Review existing systems before implementing new ones to maintain consistency.