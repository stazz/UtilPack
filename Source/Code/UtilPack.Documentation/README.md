# UtilPack.Documentation
This project currently contains one generator class, `CommandLineArgumentsDocumentationGenerator`.
This class can be used to generate a documentation, which suits the command line documentation style, from a type.
The type usually is used with `Microsoft.Extensions.Configuration.Binder` package to deserialize a configuration (command-line or from e.g. JSON file) into an object which contains properties and providing typesafe way to access the configuration values.
The generation process can further controlled by attributes contained in this package.

# Example
A typical usecase would have a configuration interface declared in a project which actually performs some of app logic, e.g.:
```cs
public interface MyAppLogicConfiguration {
   String ReadFromFile { get; }

   String WriteToFile { get; }
}

public class MyAppLogic {
   public void PerformLogic(MyAppLogicConfiguration config) {
      // ...
   }
}
```
And then in the project which contains the program start-up logic and has this project as dependency:
```cs
using UtilPack.Documentation;

public class MyAppLogicConfigurationImpl : MyAppLogicConfiguration {
   [Required, Description( ValueName="path", Description="Reads the app input from file located in this path.")]
   public String ReadFromFile { get; set; }

   [Description(ValueName="path", Description="Writes the app output to file located in this path. If not specified, will print result to stdou.")]
   public String WriteToFile { get; set; }
}
```
```cs
using Microsoft.Extensions.Configuration;
using UtilPack.Documentation;

static class Program {
   static void Main(
      String[] args
   ) {
      MyAppLogicConfiguration config = null;
      try {
         config = new ConfigurationBuilder()
            .AddCommandLine( configArgs )
            .Build()
            .Get<MyAppLogicConfigurationImpl>()
      } catch {
         
      }

      if (IsConfigValid(config) ) {
         // Config was ok, do our stuff
         new MyAppLogic().PerformLogic(config);
      } else {
         // One reason or another, config was not ok, so print help instead.
         Console.Write( new CommandLineArgumentsDocumentationGenerator()
            .GenerateParametersDocumentation( new[] // Parameter groups, typically just one
               {
                  new NamedParameterGroup(false, "options")
               },
               typeof( MyAppLogicConfigurationImpl ), // Type to get additional UtilPack.Documentation attributes ([Required, Description, etc]) from
               "MyProgram.exe", // Executable name,
               "This program takes input files and produces output files or stdout." // Short description of program
               )
            );
      }
   }

   // Please note that Microsoft.Extensions.Configuration is not aware of UtilPack.Documentation and thus does not do validation on [Required] properties etc.
   // Therefore, do it manually here (this will return true on non-null config, which has non-empty, non-null ReadFromFile property).
   private static Boolean IsConfigValid(
      MyAppLogicConfiguration config
   ) {
      return !String.IsNullOrEmpty(config?.ReadFromFile); // ReadFromFile is the required config
   }
}
```

This way, the actual logic of the application does not need to know about `UtilPack.Documentation`, which is then used only by project which performs parsing command-line arguments into a runtime object.
Note also that the configuration interface has only getters for the properties, but the class implementing the configuration interface has also setters, as the `Microsoft.Extensions.Configuration` methods require getters to set values based on command-line options.

# Distribution
See [NuGet package](http://www.nuget.org/packages/UtilPack.Documentation) for binary distribution.