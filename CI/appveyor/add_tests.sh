#!/bin/bash

set -xe

SCRIPTPATH=$(readlink -f "$0")
SCRIPTDIR=$(dirname "$SCRIPTPATH")
GIT_ROOT=$(readlink -f "${SCRIPTDIR}/../..")
BASE_ROOT=$(readlink -f "${GIT_ROOT}/..")

if [[ "${RELATIVE_NUGET_PACKAGE_DIR}" ]]; then
  NUGET_PACKAGE_DIR=$(readlink -f "${BASE_ROOT}/${RELATIVE_NUGET_PACKAGE_DIR}")
fi

if [[ "${RELATIVE_CS_OUTPUT}" ]]; then
  CS_OUTPUT=$(readlink -f "${BASE_ROOT}/${RELATIVE_CS_OUTPUT}")
fi

# Build and run test result parser
docker run --rm \
  -v "${SCRIPTDIR}/AppVeyor.Trx2Json/:/project-dir/project/:ro" \
  -v "${NUGET_PACKAGE_DIR}/:/root/.nuget/packages/:rw" \
  -v "${GIT_ROOT}/Source/Directory.Build.BuildTargetFolders.props:/project-dir/Directory.Build.props:ro" \
  -v "${CS_OUTPUT}/TestResults/:/test-report-dir/:ro" \
  -v "${CS_OUTPUT}/TestResultsAppVeyor/:/output-dir/:rw" \
  -u 0 \
  "microsoft/dotnet:${DOTNET_VERSION}-sdk-alpine" \
  dotnet run \
  -c Release \
  -p "/project-dir/project/AppVeyor.Trx2Json.csproj" \
  --no-launch-profile \
  -- \
  "/output-dir/appveyor.json" \
  "/test-report-dir/"

# Upload test results to AppVeyor
sudo chown `id -u` "${CS_OUTPUT}/TestResultsAppVeyor/appveyor.json"
cat "${CS_OUTPUT}/TestResultsAppVeyor/appveyor.json"
curl -vvv -i -X POST -d "@${CS_OUTPUT}/TestResultsAppVeyor/appveyor.json" -H "Content-Type: application/json" "${APPVEYOR_API_URL}api/tests/batch"

