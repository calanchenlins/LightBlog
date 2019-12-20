#!/usr/bin/env bash

# Usage:  ./build.sh -target=Restore -Configuration=Release

# Define default arguments.
SCRIPT="./build/build.cake"

DotNetCoreTOOLS_DIR=$HOME/.dotnet/tools
CAKE_EXE=$DotNetCoreTOOLS_DIR/dotnet-cake
GITVERSION_EXE=$DotNetCoreTOOLS_DIR/dotnet-gitversion

# dotnetCore Global Tools are installed in the following directories by default when you specify the -g (or --global) option:
# Linux/macOS    $HOME/.dotnet/tools

# Install  Cake.tool
# Make sure that Cake has been installed.
if [ ! -x "$(command -v dotnet-cake)" ]; then
    echo "Install Cake.Tool..."
    dotnet tool install --global Cake.Tool
else
    echo "dotnet-cake --version:"
    dotnet-cake --version
fi


# 将命令执行结果赋值给环境变量的两种方式
# a=`ls -l`  
# a=$(ls -l)  
# 将错误输出重定向到标准输出 2>&1
CAKE_INSTALLED_VERSION=$($CAKE_EXE --version 2>&1)
CAKE_REQUIREMENT_VERSION="0.35.0"

if [ "$CAKE_INSTALLED_VERSION" != "$CAKE_REQUIREMENT_VERSION" ]; then
    echo "wrong Version"
else
    echo "right Version"
fi

if [ ! -f "$CAKE_EXE" ]; then
    echo "Install Cake.Tool..."
    dotnet tool install --global Cake.Tool
    
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

export PATH="$PATH:$HOME/.dotnet/tools"

# Start Cake
dotnet-cake $SCRIPT "$@"
