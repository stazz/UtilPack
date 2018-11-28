#!/bin/bash

set -e

echo 'TODO deploy script!'

# Re-package UtilPack (using dotnet build /t:Pack will cause re-build even with /p:GeneratePackageOnBuild=false /p:NoBuild=true flags, so just use dotnet pack instead)
# dotnet pack /repo-dir/contents/Source/Code/UtilPack -c Release --no-build /p:IsCIBuild=true