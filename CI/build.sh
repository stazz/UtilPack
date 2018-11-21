#!/bin/bash

set -xe

# Find out the path and directory where this script resides
SCRIPTPATH=$(readlink -f "$0")
SCRIPTDIR=$(dirname "$SCRIPTPATH")
GIT_ROOT=$(readlink -f "${SCRIPTDIR}/..")
BASE_ROOT=$(readlink -f "${GIT_ROOT}/..")

# Copy required files to GIT root
cp "${SCRIPTDIR}/CISupport.props" "${GIT_ROOT}/CISupport.props"
cp "${SCRIPTDIR}/NuGet.Config" "${GIT_ROOT}/NuGet.Config.ci"

# Create key
mkdir -p "${GIT_ROOT}/Keys"
set -v
set +x
echo "${ASSEMBLY_SIGN_KEY}" | base64 -d > "${GIT_ROOT}/Keys/UtilPack.snk"
set +v
set -x

if [[ "${RELATIVE_NUGET_PACKAGE_DIR}" ]]; then
  NUGET_PACKAGE_DIR=$(readlink -f "${BASE_ROOT}/${RELATIVE_NUGET_PACKAGE_DIR}")
fi

if [[ "${RELATIVE_CS_OUTPUT}" ]]; then
  CS_OUTPUT=$(readlink -f "${BASE_ROOT}/${RELATIVE_CS_OUTPUT}")
fi

BUILD_COMMAND="dotnet build /p:Configuration=Release /p:IsCIBuild=true /t:Build;Pack /repo-dir/contents/Source/UtilPack"

if [[ "${BUILD_SCRIPT_WITHIN_CONTAINER}" ]]; then
  # Our actual build command is to invoke a script within GIT repository, and passing it the build command as parameter
  BUILD_COMMAND="/repo-dir/contents/${BUILD_SCRIPT_WITHIN_CONTAINER} ${BUILD_COMMAND}"
fi

# find "${GIT_ROOT}/Source" -maxdepth 2 -type f -name '*.csproj' -printf '/repo-dir/contents/Source/%P '
# Build within docker
docker run \
  --rm \
  -v "${GIT_ROOT}/:/repo-dir/contents/:ro" \
  -v "${CS_OUTPUT}/:/repo-dir/BuildTarget/:rw" \
  -v "${GIT_ROOT}/NuGet.Config.ci:/root/.nuget/NuGet/NuGet.Config:ro" \
  -v "${NUGET_PACKAGE_DIR}/:/root/.nuget/packages/:rw" \
  -u 0 \
  microsoft/dotnet:2.1-sdk-alpine \
  ${BUILD_COMMAND}

