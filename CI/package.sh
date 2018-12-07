#!/bin/bash

set -xe

# Find out the path and directory where this script resides
SCRIPTPATH=$(readlink -f "$0")
SCRIPTDIR=$(dirname "$SCRIPTPATH")
GIT_ROOT=$(readlink -f "${SCRIPTDIR}/..")
BASE_ROOT=$(readlink -f "${GIT_ROOT}/..")

if [[ "${RELATIVE_NUGET_PACKAGE_DIR}" ]]; then
  NUGET_PACKAGE_DIR=$(readlink -f "${BASE_ROOT}/${RELATIVE_NUGET_PACKAGE_DIR}")
fi

if [[ "${RELATIVE_CS_OUTPUT}" ]]; then
  CS_OUTPUT=$(readlink -f "${BASE_ROOT}/${RELATIVE_CS_OUTPUT}")
fi

# Using dotnet build /t:Pack will cause re-build even with /p:GeneratePackageOnBuild=false /p:NoBuild=true flags, so just use dotnet pack instead
GIT_COMMIT_HASH=$(git -C "${GIT_ROOT}" show-ref --hash HEAD)
SUCCESS_DIR="${BASE_ROOT}/package-success"
PACKAGE_COMMAND=(find /repo-dir/contents/Source/Code -mindepth 2 -maxdepth 2 -type f -name *.csproj -exec sh -c "dotnet pack -nologo -c Release --no-build /p:IsCIBuild=true /p:CIPackageVersionSuffix=${GIT_COMMIT_HASH} {}"' && touch "/success/$(basename {} .csproj)"' \;)


if [[ "${PACKAGE_SCRIPT_WITHIN_CONTAINER}" ]]; then
  # Our actual command is to invoke a script within GIT repository, and passing it the command as parameter
  PACKAGE_COMMAND=("/repo-dir/contents/${PACKAGE_SCRIPT_WITHIN_CONTAINER}" "${PACKAGE_COMMAND[@]}")
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

# Run package code within docker
rm -rf "${SUCCESS_DIR}"
docker run \
  --rm \
  -v "${GIT_ROOT}/:/repo-dir/contents/:ro" \
  -v "${CS_OUTPUT}/:/repo-dir/BuildTarget/:rw" \
  -v "${NUGET_PACKAGE_DIR}/:/root/.nuget/packages/:rw" \
  -v "${BASE_ROOT}/secrets/assembly_key.snk:/repo-dir/secrets/assembly_key.snk:ro" \
  -v "${SUCCESS_DIR}/:/success/:rw" \
  -u 0 \
  -e "THIS_TFM=netcoreapp${DOTNET_VERSION}" \
  ${ADDITIONAL_VOLUMES_STRING} \
  "microsoft/dotnet:${DOTNET_VERSION}-sdk-alpine" \
  "${PACKAGE_COMMAND[@]}"

# Verify that all test projects produced test report
PACKAGE_PROJECT_COUNT=$(find "${GIT_ROOT}/Source/Code" -mindepth 2 -maxdepth 2 -type f -name *.csproj | wc -l)
PACKAGE_SUCCESS_COUNT=$(find "${SUCCESS_DIR}" -mindepth 1 -maxdepth 1 -type f | wc -l)

if [[ ${PACKAGE_PROJECT_COUNT} -ne ${PACKAGE_SUCCESS_COUNT} ]]; then
 echo "One or more project did not package successfully." 1>&2
 exit 1
fi

# Run custom script if it is given
if [[ "$1" ]]; then
  readarray -t PACKAGE_FILES < <(find "${CS_OUTPUT}/Release/bin" -mindepth 1 -maxdepth 1 -type f -name *.nupkg)
  "$1" "${PACKAGE_FILES[@]}"
fi
