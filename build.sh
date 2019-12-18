#!/usr/bin/env bash

# Usage:  ./build.sh -target=Restore -Configuration=Release

# Define default arguments.
SCRIPT="./build/build.cake"

DotNetCoreTOOLS_DIR = $HOME/.dotnet/tools
CAKE_EXE = Join-Path $DotNetCoreTOOLS_DIR/dotnet-cake
GITVERSION_EXE = $DotNetCoreTOOLS_DIR/dotnet-gitversion

# dotnetCore Global Tools are installed in the following directories by default when you specify the -g (or --global) option:
# Linux/macOS    $HOME/.dotnet/tools

# Install  Cake.tool
# Make sure that Cake has been installed.
if [ ! -f "$CAKE_EXE" ]; then
    echo "Install Cake.Tool..."
    dotnet tool install --global Cake.Tool
    export PATH="$PATH:$HOME/.dotnet/tools"
else
    echo "dotnet-cake --version:"
    dotnet-cake --version
fi

# Install  GitVersion.Tool
if [ ! -f "$GITVERSION_EXE" ]; then
    echo "Install GitVersion.Tool..."
    dotnet tool install --global GitVersion.Tool
    export PATH="$PATH:$HOME/.dotnet/tools"
else
    echo "dotnet-gitversion /version:"
    dotnet-gitversion /version
fi
dotnet tool install --global GitVersion.Tool



# Start Cake
dotnet-cake $SCRIPT "$@"
