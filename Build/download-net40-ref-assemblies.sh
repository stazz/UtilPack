#!/bin/bash

set -xe

if [[ ! -d "$1" ]]; then
  TEMP_DIR_DL=$(mktemp -d)
  curl https://codeload.github.com/mono/reference-assemblies/zip/master --output "${TEMP_DIR_DL}/repo.zip"
  7za x -y "-o${TEMP_DIR_DL}" "${TEMP_DIR_DL}/repo.zip" reference-assemblies-master/v4.0
  mkdir "$1"
  set +e
  mv ${TEMP_DIR_DL}/reference-assemblies-master/v4.0/{.,}* "$1"
  set -e
fi