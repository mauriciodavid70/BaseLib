# BaseLib - Base Architecture Libraries

## About
BaseLib is a set of libraries designed to provide the foundational components to build a lightweight architecture for backend services in C# for .NET Core.

## Repository Structure
The BaseLib repository includes:
- [BaseLib.Core](BaseLib.Core/readme.md): The core library for the BaseLib architecture.
- [BaseLib.Core.AmazonCloud](BaseLib.Core.AmazonCloud/readme.md): Extensions and utilities for Amazon Cloud integration.
- [BaseLib.Core.MySql](BaseLib.Core.MySql/readme.md): MySQL support for BaseLib.

## Usage

BaseLib.Core can be installed using the Nuget package manager or the dotnet CLI:
```
dotnet add package BaseLib.Core
dotnet add package BaseLib.Core.AmazonCloud
dotnet add package BaseLib.Core.MySql
```

## Version Compatibility

| BaseLib.Core | BaseLib.Core.AmazonCloud | BaseLib.Core.MySql | .NET |
|---|---|---|---|
| 3.1.x | 3.1.x | 3.1.x | net8.0 |

All packages within the same minor version are compatible with each other. The `AmazonCloud` and `MySql` packages depend on `BaseLib.Core` of the same minor version (`3.1.*`).

## Contributing

Contributions are welcome. Please follow these steps:

1. **Fork** the repository and create a feature branch from `master`.
2. **Write or update tests** in `BaseLib.Core.Tests` for any changed behaviour.
3. **Build and test** locally before submitting:
   ```bash
   dotnet build
   dotnet test
   ```
4. **Document your changes**:
   - Add XML doc comments (`/// <summary>`) to all new public types and members.
   - Update the relevant `readme.md` if you add or change a feature.
5. **Open a pull request** against `master` with a clear description of what changed and why.

### Documentation standards
- Public interfaces and classes must have `<summary>` XML doc comments.
- Non-obvious parameters must have `<param>` comments.
- New features should include a usage example in the module `readme.md`.

## License
BaseLib is licensed under the MIT License. See the LICENSE file in the repository for more details.
