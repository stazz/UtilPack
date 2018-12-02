#!/bin/bash
set -xe

for PACKAGE_PATH in "$@"; do
  appveyor PushArtifact "${PACKAGE_PATH}" -FileName "$(basename "${PACKAGE_PATH}")"
done
