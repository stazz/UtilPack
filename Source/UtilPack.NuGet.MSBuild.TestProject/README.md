# Test project folder for UtilPack.NuGet.MSBuild task factory

This folder contains one essential project file (```GenerateSQL.build```), which aims to demonstrate on how to use the UtilPack.NuGet.MSBuild task factory.
The folder also contains other files, which are part of infrastructure of easy setup.

## Quick guide
Using MSBuild commandline, run ```RunDemo``` target on ```UtilPack.NuGet.MSBuild.Demo.build``` file.
You should end up with ```output.sql``` file, which is produced using the code in ```DemoSQLGenerator``` folder.
For subsequent runs, it is enough to run ```GenerateSQLFile``` target on ```GenerateSQL.build``` file.

## More detailed information
The ```UtilPack.NuGet.MSBuild.Demo.build``` file performs three steps, the first two of which are one-time steps for setting up infrastructure.

1. The ```DemoSQLGenerator/DemoSQLGenerator.csproj``` project gets restored and built.
This project file contains demonstration code which generates a very simple SQL statements to create one schema and one table.
This step will restore ```SQLGenerator.Usage``` and ```SQLGenerator``` package in order for compilation to be successful.

2. The ```GenerateSQL.build``` project gets restored.
This will restore ```UtilPack.NuGet.MSBuild``` package which will be used in next step.

3. The ```GenerateSQLFile``` target is run on ```GenerateSQL.build``` project.
This will invoke the ```UtilPack.NuGet.MSBuild``` task factory so that it will use task in ```SQLGenerator.MSBuild``` package.
This task will then in turn load ```SQLGenerator.PostgreSQL``` package (which contains concrete implementation of ```SQLVendor``` interface used in ```DemoSQLGenerator/DemoSQLGenerator.csproj```) and the DLL of ```DemoSQLGenerator/DemoSQLGenerator.csproj``` created in step 1 in order to generate SQL statements and write them to ```output.sql``` file.

As mentioned, the first two steps are just setting up the infrastructure, and the final step is the one which should be repeated if no other changes in the system happen.
However, if you modify code in ```DemoSQLGenerator/.cs```, step 1 will need to be repeated.
Step 2 will only need to be repeated if version to ```UtilPack.NuGet.MSBuild``` is changed or the package is deleted from local repository.

## What Next?
Feel free to explore advanced features of ```UtilPack.NuGet.MSBuild``` in [here](../UtilPack.NuGet.MSBuild).
Alternatively, evolve SQL generation task to generate more complex SQL files.
When those SQL files need to be run to database, you might be interested in [CBAM.SQL.MSBuild](/CometaSolutions/CBAM/tree/develop/Source/CBAM.SQL.MSBuild) MSBuild task, which also is executable by ```UtilPack.NuGet.MSBuild``` task factory.
