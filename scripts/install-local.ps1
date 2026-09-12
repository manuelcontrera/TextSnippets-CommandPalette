param(
    [string]$Configuration = "Release"
)

$PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RootDir = Join-Path $PSScriptRoot ".."
$BuildScript = Join-Path $PSScriptRoot "build.ps1"
$ManifestPath = Join-Path $RootDir "src\SnippetsCmdPal\bin\$Configuration\net10.0-windows10.0.26100.0\win-x64\AppxManifest.xml"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  Installing / Registering Text Snippets for Command Palette     " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# 1. Build and bundle
& "$BuildScript" -Configuration $Configuration
if ($LASTEXITCODE -ne 0) {
    Write-Error "Build step failed."
    exit 1
}

# 2. Register package in Windows
Write-Host "Registering sparse package: $ManifestPath" -ForegroundColor Cyan
try {
    Add-AppxPackage -Register "$ManifestPath" -ForceApplicationShutdown -ErrorAction Stop
    Write-Host "[OK] Package registered successfully!" -ForegroundColor Green
} catch {
    Write-Host "[WARNING] Registration error: $_" -ForegroundColor Red
    Write-Host "Ensure 'Developer Mode' is turned ON in Windows Settings (Settings -> System -> For developers)." -ForegroundColor Yellow
    exit 1
}

# 3. Reload notification
Write-Host "`nTo use in PowerToys Command Palette:" -ForegroundColor Yellow
Write-Host "  1. Open Command Palette (Alt + Space or your configured hotkey)." -ForegroundColor White
Write-Host "  2. Run 'Reload Command Palette extensions' or search for 'Text Snippets'." -ForegroundColor White
Write-Host "  3. Ready! Test typing '!email' or '!date' in any application." -ForegroundColor Green
