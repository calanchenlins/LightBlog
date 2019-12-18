# Usage:  ./build.ps1 -target=Restore -Configuration=Release -Runtime=linux-x64
[string]$SCRIPT = './build/build.cake'

$DotNetCoreTOOLS_DIR = Join-Path $env:USERPROFILE ".dotnet/tools"
$CAKE_EXE = Join-Path $DotNetCoreTOOLS_DIR "dotnet-cake.exe"
$GITVERSION_EXE = Join-Path $DotNetCoreTOOLS_DIR "dotnet-gitversion.exe"

# Install  cake.tool
if (!(Test-Path $CAKE_EXE)) {
    echo "Install Cake.Tool..."
    dotnet tool install --global Cake.Tool
}
else{
    echo "dotnet-cake --version:"
    dotnet-cake --version
}

# Install  GitVersion.Tool
if (!(Test-Path $GITVERSION_EXE)) {
    echo "Install GitVersion.Tool..."
    dotnet tool install --global GitVersion.Tool
}
else{
    echo "dotnet-gitversion /version:"
    dotnet-gitversion /version
}


dotnet-cake $SCRIPT $ARGS