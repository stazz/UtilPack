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

# Run tests with hard-coded trx format, for now.
TEST_COMMAND=(find /repo-dir/contents/Source/Tests -mindepth 2 -maxdepth 2 -type f -name *.csproj -exec sh -c 'dotnet test -c Release --no-build --logger trx\;LogFileName=/repo-dir/BuildTarget/TestResults/$(basename {} .csproj).trx /p:IsCIBuild=true {}' \;)


if [[ "${TEST_SCRIPT_WITHIN_CONTAINER}" ]]; then
  # Our actual command is to invoke a script within GIT repository, and passing it the command as parameter
  TEST_COMMAND=("/repo-dir/contents/${TEST_SCRIPT_WITHIN_CONTAINER}" "${TEST_COMMAND[@]}")
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

# Run tests code within docker
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
  "${TEST_COMMAND[@]}"

# Verify that all test projects produced test report
TEST_PROJECT_COUNT=$(find "${GIT_ROOT}/Source/Tests" -mindepth 2 -maxdepth 2 -type f -name *.csproj | wc -l)
TEST_REPORT_COUNT=$(find "${CS_OUTPUT}/TestResults" -mindepth 1 -maxdepth 1 -type f -name *.trx | wc -l)

if [[ ${TEST_PROJECT_COUNT} -ne ${TEST_REPORT_COUNT} ]]; then
 echo "One or more project did not produce test report successfully." 1>&2
 exit 1
fi
  
# Verify that all tests in all test reports are passed.
# Enumerate all .trx files, for each of those execute Python one-liner to get amount of executed and passed tests, print them out and save to array.
# Each array element is two numbers separated by space character - first is executed count, second is passed count.
readarray -t TEST_RESULTS < <(find "${CS_OUTPUT}/TestResults" -name *.trx -exec python3 -c "from xml.etree.ElementTree import ElementTree; doc = ElementTree(file='{}'); docNS = doc.getroot().tag.split('}')[0].strip('{'); counters = doc.find('test_ns:ResultSummary/test_ns:Counters', { 'test_ns': docNS }); print(counters.attrib['executed'] + ' ' + counters.attrib['passed']);" \;)
# Walk each array element and make sure that first number matches second
for TEST_RESULT in "${TEST_RESULTS[@]}"; do
  EXECUTED_AND_PASSED=(${TEST_RESULT})
  if [[ "${EXECUTED_AND_PASSED[0]}" -ne "${EXECUTED_AND_PASSED[1]}" ]]; then
    exit 1
  fi
done
