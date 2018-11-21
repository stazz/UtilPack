#!/bin/sh

# This script is intended to run within Docker container with .NET SDK, and actual command as parameters.
# Therefore all folder names etc are constants.

set -xe

# Build all projects
$@

# dotnet tool install -g NuGetUtils.Tool.Restore
# /root/.dotnet/tools/nuget-restore /PackageID=runtime.alpine.3.6-x64.Microsoft.NETCore.ILDAsm /PackageVersion=2.2.0-preview1-26425-01

/root/.nuget/packages/runtime.alpine.3.6-x64.microsoft.netcore.ildasm/2.2.0-preview1-26425-01/runtimes/alpine.3.6-x64/native/ildasm


/c/Users/Staz/.nuget/packages/runtime.alpine.3.6-x64.microsoft.netcore.ildasm/2.2.0-preview1-26425-01/runtimes/alpine.3.6-x64/native/ildasm
