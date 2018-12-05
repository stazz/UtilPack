#!/bin/bash

set -xe

SCRIPTPATH=$(readlink -f "$0")
SCRIPTDIR=$(dirname "$SCRIPTPATH")
GIT_ROOT=$(readlink -f "${SCRIPTDIR}/../..")
BASE_ROOT=$(readlink -f "${GIT_ROOT}/..")

DOTNET_SDK_IMAGE="microsoft/dotnet:${DOTNET_VERSION}-sdk-alpine"
DOCKER_FILE_DIR="${BASE_ROOT}/docker-images"
DOTNET_SDK_FILE="${DOCKER_FILE_DIR}/dotnet-sdk.tar"

mkdir -p "${DOCKER_FILE_DIR}"
# First, check if the save image file exists
if [[ -f "${DOTNET_SDK_FILE}" ]]; then
  # Load image
  docker image load -i "${DOTNET_SDK_FILE}"

  # Get image ID
  DOTNET_SDK_IMAGE_ID="$(docker image inspect -f '{{ .Id }}' "${DOTNET_SDK_IMAGE}")"
  
  # Pull image
  docker pull "${DOTNET_SDK_IMAGE}"

  # Get new ID
  DOTNET_SDK_IMAGE_ID_NEW="$(docker image inspect -f '{{ .Id }}' "${DOTNET_SDK_IMAGE}")"

  # Save if new ID is different (we pulled new version)
  if [[ "${DOTNET_SDK_IMAGE_ID}" != "${DOTNET_SDK_IMAGE_ID_NEW}" ]]; then
    docker image save -o "${DOTNET_SDK_FILE}" "${DOTNET_SDK_IMAGE}"
  fi
else
  # Pull image
  docker pull "${DOTNET_SDK_IMAGE}"

  # Save image to disk
  docker image save -o "${DOTNET_SDK_FILE}" "${DOTNET_SDK_IMAGE}" 
fi


