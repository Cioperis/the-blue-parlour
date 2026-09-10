param([string]$Output = "$PSScriptRoot/../artifacts/win-x64")
$ErrorActionPreference = 'Stop'
dotnet publish "$PSScriptRoot/../src/BlueParlour.Desktop/BlueParlour.Desktop.csproj" -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o $Output
if ($LASTEXITCODE -ne 0) { throw 'Publish failed.' }
