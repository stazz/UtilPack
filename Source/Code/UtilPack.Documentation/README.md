# UtilPack.Documentation
This project currently contains one generator class, `CommandLineArgumentsDocumentationGenerator`.
This class can be used to generate a documentation, which suits the command line documentation style, from a type.
The type usually is used with `Microsoft.Extensions.Configuration.Binder` package to deserialize a configuration (command-line or from e.g. JSON file) into an object which contains properties and providing typesafe way to access the configuration values.
The generation process can further controlled by attributes contained in this package.

# Distribution
See [NuGet package](http://www.nuget.org/packages/UtilPack.Documentation) for binary distribution.