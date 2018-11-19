#!/bin/bash

set -xe

# Find out the path and directory where this script resides
SCRIPTPATH=$(readlink -f "$0")
SCRIPTDIR=$(dirname "$SCRIPTPATH")
GIT_ROOT=$(readlink -f "${SCRIPTDIR}/..")

# Copy required files to GIT root
cp "${SCRIPTDIR}/CISupport.props" "${GIT_ROOT}/CISupport.props"
cp "${SCRIPTDIR}/NuGet.Config" "${GIT_ROOT}/NuGet.Config.ci"

# Create key
set +x
mkdir "${SCRIPTDIR}/Keys"
echo "${ASSEMBLY_SIGN_KEY}" | base64 -d > "${SCRIPTDIR}/Keys/UtilPack.snk"
set -x

# find "${GIT_ROOT}/Source" -maxdepth 2 -type f -name '*.csproj' -printf '/repo-dir/contents/Source/%P '
# Build within docker
docker run \
  --rm \
  -v "${GIT_ROOT}/:/repo-dir/contents/:ro" \
  -v "${CS_OUTPUT}:/repo-dir/BuildTarget/:rw" \
  -v "${GIT_ROOT}/NuGet.Config.ci:/root/.nuget/NuGet/NuGet.Config:ro" \
  -v "/nuget_package_dir/:/root/.nuget/packages/:rw" \
  -u 0 \
  microsoft/dotnet:2.1-sdk-alpine \
  dotnet \
  build \
  '/p:Configuration=Release' \
  '/p:IsCIBuild=true' \
  '/t:Build;Pack' \
  '/repo-dir/contents/Source/UtilPack.Logging'

