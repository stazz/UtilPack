#!/bin/sh

# This script is intended to run within Docker container with .NET SDK, and actual command as first parameter.
# Therefore all folder names etc are constants.

set -xe

# Build all projects
$@

# TODO ILDAsm -> ILAsm with the .il files

