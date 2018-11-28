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

if [[ "${RELATIVE_NUGET_PACKAGE_DIR}" ]]; then
  NUGET_PACKAGE_DIR=$(readlink -f "${BASE_ROOT}/${RELATIVE_NUGET_PACKAGE_DIR}")
fi

if [[ "${RELATIVE_CS_OUTPUT}" ]]; then
  CS_OUTPUT=$(readlink -f "${BASE_ROOT}/${RELATIVE_CS_OUTPUT}")
fi

# TODO what to put as log filename since {} will just be full path instead of e.g. basename...
TEST_COMMAND="find /repo-dir/contents/Source/Tests -mindepth 2 -maxdepth 2 -type f -name *.csproj -exec dotnet test -c Release --no-build --logger trx;LogFileName=/repo-dir/BuildTarget/TestResults/UtilPack.trx /p:IsCIBuild=true {} ;"

if [[ "${TEST_SCRIPT_WITHIN_CONTAINER}" ]]; then
  # Our actual build command is to invoke a script within GIT repository, and passing it the build command as parameter
  TEST_COMMAND="/repo-dir/contents/${TEST_SCRIPT_WITHIN_CONTAINER} ${TEST_COMMAND}"
fi

if [[ "${ADDITIONAL_VOLUME_DIRECTORIES}" ]]; then
  IFS=', ' read -r -a volume_dir_array <<< "${ADDITIONAL_VOLUME_DIRECTORIES}"
  ADDITIONAL_VOLUMES_STRING=
  for volume_dir in "${volume_dir_array[@]}"
  do
    if [[ "${ADDITIONAL_VOLUMES_STRING}" ]]; then
      ADDITIONAL_VOLUMES_STRING+=" "
    fi
    
    ADDITIONAL_VOLUMES_STRING+="-v ${BASE_ROOT}/${volume_dir}:/repo-dir/${volume_dir}/:ro"
  done
fi

# Build code within docker
docker run \
  --rm \
  -v "${GIT_ROOT}/:/repo-dir/contents/:ro" \
  -v "${CS_OUTPUT}/:/repo-dir/BuildTarget/:rw" \
  -v "${GIT_ROOT}/NuGet.Config.ci:/root/.nuget/NuGet/NuGet.Config:ro" \
  -v "${NUGET_PACKAGE_DIR}/:/root/.nuget/packages/:rw" \
  -u 0 \
  -e THIS_TFM=netcoreapp2.1 \
  ${ADDITIONAL_VOLUMES_STRING} \
  microsoft/dotnet:2.1-sdk-alpine \
  ${TEST_COMMAND}
  
# Because find does not return non-0 exit code even when its -exec command does, we need to make sure that we have equal amount of test reports...? as there are projects
# SOURCE_PROJECT_COUNT=$(find "${GIT_ROOT}/Source/Tests" -mindepth 2 -maxdepth 2 -type f -name *.csproj | wc -l)
# SOURCE_ARTIFACT_COUNT=$(find "${CS_OUTPUT}/Release/bin" -mindepth 2 -maxdepth 2 -type f -name *.nupkg | wc -l)

# if [[ ${SOURCE_PROJECT_COUNT} -ne ${SOURCE_ARTIFACT_COUNT} ]]; then
#  echo "One or more project did not build successfully." 1>&2
#  exit 1
# fi