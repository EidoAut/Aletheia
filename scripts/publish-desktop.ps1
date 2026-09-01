[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",

    [string]$Runtime = "win-x64",

    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repositoryRoot "src/Aletheia.Desktop/Aletheia.Desktop.csproj"
$artifactsRoot = Join-Path $repositoryRoot "artifacts"
$outputDirectory = Join-Path $artifactsRoot "Aletheia.Desktop-$Runtime"
$archivePath = "$outputDirectory.zip"
$selfContainedValue = if ($SelfContained.IsPresent) { "true" } else { "false" }
$resolvedArtifactsRoot = [System.IO.Path]::GetFullPath($artifactsRoot)
$resolvedOutputDirectory = [System.IO.Path]::GetFullPath($outputDirectory)
$resolvedArchivePath = [System.IO.Path]::GetFullPath($archivePath)
if (-not $resolvedOutputDirectory.StartsWith($resolvedArtifactsRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to clean publish directory outside artifacts: $resolvedOutputDirectory"
}

if (-not $resolvedArchivePath.StartsWith($resolvedArtifactsRoot + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to replace publish archive outside artifacts: $resolvedArchivePath"
}

Push-Location $repositoryRoot
try {
    if (Test-Path $outputDirectory) {
        Remove-Item -LiteralPath $outputDirectory -Recurse -Force
    }

    if (Test-Path $archivePath) {
        Remove-Item -LiteralPath $archivePath -Force
    }

    dotnet publish $project `
        --configuration $Configuration `
        --runtime $Runtime `
        --self-contained $selfContainedValue `
        --output $outputDirectory `
        -p:EnableWindowsTargeting=true

    $executable = Join-Path $outputDirectory "Aletheia.Desktop.exe"
    if (-not (Test-Path $executable)) {
        throw "Desktop publish did not produce $executable."
    }

    Compress-Archive -Path (Join-Path $outputDirectory "*") -DestinationPath $archivePath -CompressionLevel Optimal
    Write-Host "Desktop package: $archivePath"
}
finally {
    Pop-Location
}
