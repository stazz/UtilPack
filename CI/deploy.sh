#!/bin/bash

set -xe

SCRIPTPATH=$(readlink -f "$0")
SCRIPTDIR=$(dirname "$SCRIPTPATH")
GIT_ROOT=$(readlink -f "${SCRIPTDIR}/..")
BASE_ROOT=$(readlink -f "${GIT_ROOT}/..")

if [[ "${RELATIVE_CS_OUTPUT}" ]]; then
  CS_OUTPUT=$(readlink -f "${BASE_ROOT}/${RELATIVE_CS_OUTPUT}")
fi

# Get current GIT branch
# git rev-parse --abbrev-ref HEAD <- won't work, since the repo will be in "detached HEAD state" after CI tool checks out the commit.
# git for-each-ref --format='%(objectname) %(refname:short)' refs/heads | awk "/^$(git rev-parse HEAD)/ {print \$2}" <- is nice, but only works for tip commits. Sometimes we might not be at the tip.
# git rev-list --first-parent <branchname> <- this shows all commit hashes for a specific branch, therefore allows us to reliably determine whether we are on some specific branch, tip or not.
# Don't fail on error (e.g. if master branch does not exist)
GIT_COMMIT_HASH="$(git -C "${GIT_ROOT}" show --quiet --format=format:%H HEAD)"
set +e
COMMIT_HASH_IN_MASTER="$(git -C "${GIT_ROOT}" rev-list --first-parent master -- 2>/dev/null | grep -o "${GIT_COMMIT_HASH}")"
set -e
if [[ "${GIT_COMMIT_HASH}" == "${COMMIT_HASH_IN_MASTER}" ]]; then
  # Get all tags for this commit
  readarray -t CURRENT_TAGS < <(git -C "${GIT_ROOT}" tag --points-at)
  
  if [[ "${#CURRENT_TAGS[@]}" -gt "0" ]]; then
    # Tags were defined for this commit, all tags must match the filename of the .nupkg exactly in order to get pushed.
    # Artifacts should have been collected to CI server by this point, so now we can just copy the packages that should be pushed to a directory, and push all at once with one command
    PUSH_DIR="${BASE_ROOT}/push-source"
    mkdir "${PUSH_DIR}"
    for CURRENT_TAG in "${CURRENT_TAGS[@]}"; do
      # This will return non-zero, failing whole script, if file does not exist
      cp "${CS_OUTPUT}/Release/bin/${CURRENT_TAG}.nupkg" "${PUSH_DIR}/"
    done

    ADDITIONAL_DOCKER_ARGS=()
    if [[ "${DEPLOY_NUGET_CONFIG_FILE}" ]]; then
      ADDITIONAL_DOCKER_ARGS+=('-v' "${GIT_ROOT}/${DEPLOY_NUGET_CONFIG_FILE}:/push-dir/NuGet.Config:ro")
    fi

    ADDITIONAL_PUSH_ARGS=()
    if [[ "${DEPLOY_NUGET_SOURCE}" ]]; then
      ADDITIONAL_PUSH_ARGS+=('--source' "${DEPLOY_NUGET_SOURCE}")
    fi
    if [[ "${DEPLOY_NUGET_SYMBOL_SOURCE}" ]]; then
      ADDITIONAL_PUSH_ARGS+=('--symbol-source' "${DEPLOY_NUGET_SYMBOL_SOURCE}")
    fi
    if [[ "${DEPLOY_NUGET_NO_SYMBOLS}" ]]; then
      ADDITIONAL_PUSH_ARGS+=('--no-symbols')
    fi

    if [[ "${DEPLOY_NUGET_NO_SERVICE_ENDPOINT}" ]]; then
      ADDITIONAL_PUSH_ARGS+=('--no-service-endpoint')
    fi

    # Turn off expanding variables as we are dealing with secret values.
    set -v
    set +x

    if [[ "${DEPLOY_NUGET_API_KEY}" ]]; then
      ADDITIONAL_PUSH_ARGS+=('--api-key' "${DEPLOY_NUGET_API_KEY}")
    fi
    if [[ "${DEPLOY_NUGET_SYMBOL_API_KEY}" ]]; then
      ADDITIONAL_PUSH_ARGS+=('--symbol-api-key' "${DEPLOY_NUGET_SYMBOL_API_KEY}")
    fi

    # All .nupkg files have been copied, invoke the command via docker
    docker run --rm \
      -v "${PUSH_DIR}/:/push-dir/content/:ro" \
      "${ADDITIONAL_DOCKER_ARGS[@]}" \
      -u 0 \
      -w "/push-dir/content" \
      "microsoft/dotnet:${DOTNET_VERSION}-sdk-alpine" \
      dotnet nuget push \
      '*.nupkg' \
      --force-english-output \
      "${ADDITIONAL_PUSH_ARGS[@]}"

    set +v
    set -x
  fi
fi
