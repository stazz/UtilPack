# UtilPack.NuGet

This project contains few useful utility classes which make life easier when working with [NuGet library](https://github.com/NuGet/NuGet.Client).
Perhaps the most important type in this project is the `BoundRestoreCommandUser` class, which provides an easy way to execute the NuGet restore command.
The name of the class stems from the decision to bind the NuGet framework that restore command is executed against, along with binding also the current runtime (win, unix, osx).
There are also several extension methods for that class to help with assembly files of the restored packages.

The `TextWriterLogger` class implements the `ILogger` interface from the NuGet.Client package, and allows controlled and configurable way of writing the NuGet log messages to a `System.IO.TextWriter` object.
The `Console.Out` and `Console.Error` properties both extend the `System.IO.TextWriter` class, making this logger useable to log messages to console.

Then there is `AgnosticFrameworkLoggerWrapper`, which is an extremely specialized implementation of the `ILogger` interface - it will filter out error messages about incompatibility with the `Agnostic` NuGet framework when performing Restore command.
This is particularly useful when the logging should be used, and errors should be logged, except for that one specific "error" - since it is not really an error.

Finally, there are some extension methods in the [Frameworks.cs](./Framework.cs) file, which e.g. can parse NuGet framework from the `TargetFrameworkAttribute` applied to the assembly.

# Distribution

See [NuGet package](http://www.nuget.org/packages/UtilPack.NuGet) for binary distribution.