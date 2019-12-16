# Usage:  ./build.ps1 -target=Restore -Configuration=Release -Runtime=linux-x64
[string]$SCRIPT = './build/build.cake'

# Install  cake.tool
dotnet tool install --global cake.tool

dotnet cake $SCRIPT $ARGS