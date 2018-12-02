#!/bin/bash

set -xe

# Get current GIT branch
CURRENT_BRANCH=$(git rev-parse --abbrev-ref HEAD)
if [[ "${CURRENT_BRANCH}" -eq "master" ]]; then
  # Get all tags for this commit
  readarray -t CURRENT_TAGS < <(git tag --points-at)
  
  if [[ "${#CURRENT_TAGS[@]}" -gt "0" ]]; then
    # Tags were defined for this commit, all tags must match the filename of the .nupkg exactly in order to get pushed.
    for CURRENT_TAG in "${CURRENT_TAGS[@]}"; do
      # TODO
    done
  fi
fi
