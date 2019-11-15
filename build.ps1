# Usage:  ./build.ps1 -target=Restore -Configuration=Release
[string]$SCRIPT = './build/build.cake'

# Install  cake.tool
dotnet tool install --global cake.tool

dotnet cake $SCRIPT $ARGS