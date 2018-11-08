# NuGetUtils.Lib.EntryPoint

This project currently contains one attribute, `ConfiguredEntryPointAttribute`.
It can be used together with [nuget-exec](../NuGetUtils.Tool.Exec) .NET Core Global Tool to control how the tool finds entrypoint method.

The tool will examine assembly's entrypoint information when it tries to detect the entrypoint method, but before that it checks for the `ConfiguredEntryPointAttribute` attribute.
This way, the entrypoint method can be something else than assembly's entrypoint method.
The typical scenario looks something like this:
```cs
[assembly: ConfiguredEntryPoint( typeof( MyClass ), nameof( MyClass.MyMethod ) )]

public class MyClass {
   public static void MyMethod() {
      // your code here...
   }
}
```

The method to be executed by the tool can also contain parameters, the types of which may be something else than typical `String[]`.
See the [nuget-exec](../NuGetUtils.Tool.Exec) for more information.

# Distribution

See [NuGet package](http://www.nuget.org/packages/NuGetUtils.Lib.EntryPoint) for binary distribution.