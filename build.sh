#!/usr/bin/env bash

# Usage:  ./build.sh -target=Restore -Configuration=Release

# Define default arguments.
SCRIPT="./build/build.cake"

# Install  cake.tool
dotnet tool install --global cake.tool
export PATH="$PATH:$HOME/.dotnet/tools"

# Start Cake
dotnet cake $SCRIPT "$@"
