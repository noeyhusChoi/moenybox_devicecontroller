param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$RepoUrl = "https://github.com/noeyhusChoi/moenybox_devicecontroller",
    [string]$Channel = "win",
    [switch]$Upload,
    [switch]$PublishRelease
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "IdScannerTool\IdScannerTool.csproj"
$publishDir = Join-Path $repoRoot "artifacts\publish\IdScannerTool"
$releaseDir = Join-Path $repoRoot "artifacts\velopack"
$toolDir = Join-Path $repoRoot ".tools\vpk"
$packId = "MBoxIDScanner"
$mainExe = "M-Box ID Scanner.exe"

if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
}

if (Test-Path $releaseDir) {
    Remove-Item $releaseDir -Recurse -Force
}

if (-not (Test-Path $toolDir)) {
    New-Item -ItemType Directory -Path $toolDir -Force | Out-Null
}

dotnet tool install --tool-path $toolDir vpk --version 0.0.1298 | Out-Host

dotnet publish $projectPath `
    -c Release `
    -r win-x86 `
    --self-contained true `
    -p:Platform=x86 `
    -p:Version=$Version `
    -o $publishDir | Out-Host

$vpk = Join-Path $toolDir "vpk.exe"

& $vpk pack `
    --packId $packId `
    --packVersion $Version `
    --packDir $publishDir `
    --mainExe $mainExe `
    --packAuthors Moneybox `
    --packTitle "M-Box ID Scanner" `
    --channel $Channel `
    --outputDir $releaseDir | Out-Host

if (-not $Upload) {
    Write-Host "Velopack package created at: $releaseDir"
    exit 0
}

if ([string]::IsNullOrWhiteSpace($env:VELOPACK_RELEASE_TOKEN)) {
    throw "VELOPACK_RELEASE_TOKEN environment variable is required when -Upload is used."
}

$uploadArgs = @(
    "upload", "github",
    "--outputDir", $releaseDir,
    "--repoUrl", $RepoUrl,
    "--token", $env:VELOPACK_RELEASE_TOKEN,
    "--channel", $Channel,
    "--merge",
    "--tag", "v$Version",
    "--releaseName", "M-Box ID Scanner v$Version"
)

if ($PublishRelease) {
    $uploadArgs += "--publish"
}

& $vpk @uploadArgs | Out-Host
