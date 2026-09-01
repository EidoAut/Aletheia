[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $repositoryRoot "Aletheia.sln"

Push-Location $repositoryRoot
try {
    dotnet --version
    dotnet restore $solution -p:EnableWindowsTargeting=true
    dotnet build $solution --configuration $Configuration --no-restore -p:EnableWindowsTargeting=true
    dotnet test $solution --configuration $Configuration --no-build --no-restore -p:EnableWindowsTargeting=true --logger "console;verbosity=normal"
}
finally {
    Pop-Location
}
