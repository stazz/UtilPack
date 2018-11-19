#!/bin/bash

set -xe

# Find out the path and directory where this script resides
SCRIPTPATH=$(readlink -f "$0")
SCRIPTDIR=$(dirname "$SCRIPTPATH")
GIT_ROOT=$(readlink -f "${SCRIPTPATH}/.." )

# Copy required files to GIT root
cp "${SCRIPTDIR}/CISupport.props" "${GIT_ROOT}/CISupport.props"
cp "${SCRIPTDIR}/NuGet.config" "${GIT_ROOT}/NuGet.Config.ci"

# Build within docker
docker run \
  --rm \
  -v "${GIT_ROOT}/:/repo-dir/contents/:ro" \
  -v "/output/:/repo-dir/BuildTarget/:rw" \
  -v "${GIT_ROOT}/NuGet.config.ci:/root/.nuget/NuGet/NuGet.Config:ro" \
  -v "/nuget_package_dir/:/root/.nuget/packages/:rw" \
  microsoft/dotnet:2.1-sdk-alpine \
  dotnet \
  build \
  '/p:Configuration=Release' \
  '/p:IsCIBuild=true' \
  '/t:Build;Pack' \
  '/repo-dir/contents/Source/UtilPack.Logging'

