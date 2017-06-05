# Test project folder for UtilPack.NuGet.MSBuild task factory

This folder contains one .csproj project file, which aims to demonstrate on how to use the UtilPack.NuGet.MSBuild task factory.

## Quick guide
First, run ```Restore``` target as its own separate command.
Then run ```RunExecuteSQLStatements``` target and specify valid options to ```ConnectionConfigurationFilePath``` and ```SQLStatementsFilePath``` properties.
Currently, this example requires setting up PostgreSQL database in order to complete without errors, but right now there are no other examples.

## More detailed information
The ```UtilPack.NuGet.MSBuild.TestProject.csproj``` project file contains information about the actual task to be executed.
The ```Restore``` target is required in order to NuGet set up the .g.props and other files, so that ```UtilPackNuGetMSBuildAssemblyPath``` property will become visible.
Once the restore is complete, it will be required to run it again only if you update to other version of ```UtilPack.NuGet.MSBuild``` package.
The ```Restore``` target can not be bundled with ```RunExecuteSQLStatements``` by specifying ```Restore;RunExecuteSQLStatements```, as ```UsingTask``` element won't be recalculated.

The ```RunExecuteSQLStatements``` target will use the ```CBAM.SQL.MSBuild``` package to execute SQL statements.
The ```ConnectionConfigurationFilePath``` property should point to JSON file, which contains a single JSON object, with ```Host```, ```Port```, ```Database```, ```Username```, and ```Password``` properties.
The ```SQLStatementsFilePath``` should be a path to a file containing some SQL statements which are desired to be run to database.

Once started, the ```CBAM.SQL.MSBuild``` package will dynamically load ```CBAM.SQL.PostgreSQL.Implementation``` package, and use the supplied information to execute SQL statements from file to the database.
If any of the required package is missing from local NuGet repository, the task factory will restore it.
Restoration process can be customized by specifying NuGet configuration file via ```NuGetConfigurationFile``` element in ```NuGetTaskInfo``` element.
