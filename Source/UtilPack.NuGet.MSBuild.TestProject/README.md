# Test project folder for UtilPack.NuGet.MSBuild task factory

This folder contains two .csproj project files, which aim to demonstrate on how to use the UtilPack.NuGet.MSBuild task factory.

## Quick guide
Run ```Restore``` target for both projects exactly once (order does not matter).
Then run ```RunExecuteSQLStatements``` target for ```UtilPack.NuGet.MSBuild.TestProject.csproj``` and specify valid options to ```ConnectionConfigurationFilePath``` and ```SQLStatementsFilePath``` properties.
Currently, this example requires setting up PostgreSQL database in order to complete without errors, but right now there are no other examples.

## The .References project
The ```UtilPack.NuGet.MSBuild.TestProject.References.csproj``` project file contains information about all used NuGet package via the ```<PackageReference>``` elements.
The existance of this separate project file is to avoid any cluttering in the actual project file containing task execution, since target framework etc properties might be different.
Once UtilPack.NuGet.MSBuild project learns how to restore missing packages, the will be no need for this separate .References project anymore, and it may be deleted.

## The actual project
The ```UtilPack.NuGet.MSBuild.TestProject.csproj``` project file contains information about the actual task to be executed.
The ```Restore``` command is required in order to NuGet set up the .g.props and other files, so that ```UtilPackNuGetMSBuildAssemblyPath``` property will become visible.
Once the restore is complete, it will be required to run it again only if you update to other version of ```UtilPack.NuGet.MSBuild``` package.

The project contains ```RunExecuteSQLStatements``` target, which will use the ```CBAM.SQL.MSBuild``` package to execute SQL statements.
The ```ConnectionConfigurationFilePath``` property should point to JSON file, which contains a single JSON object, with ```Host```, ```Port```, ```Database```, ```Username```, and ```Password``` properties.
The ```SQLStatementsFilePath``` should be a path to a file containing some SQL statements which are desired to be run to database.

Once started, the ```CBAM.SQL.MSBuild``` package will dynamically load ```CBAM.SQL.PostgreSQL.Implementation``` package, and use the supplied information to execute SQL statements from file to the database.
