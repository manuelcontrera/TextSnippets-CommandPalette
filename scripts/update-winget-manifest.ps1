param(
    [string]$Version = "1.0.1",
    [string]$MsixPath = ""
)

$ErrorActionPreference = "Stop"
$PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RootDir = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$WingetDir = Join-Path $RootDir "winget"

if (-not $MsixPath) {
    $MsixPath = Join-Path $RootDir "dist\TextSnippets-CommandPalette-$Version.0-x64.msix"
    if (-not (Test-Path $MsixPath)) {
        $MsixPath = Join-Path $RootDir "dist\TextSnippets-CommandPalette-$Version-x64.msix"
    }
}

if (-not (Test-Path $MsixPath)) {
    throw "MSIX file not found at $MsixPath. Run scripts\package-msix.ps1 first."
}

Write-Host "Calculating SHA256 hashes from $MsixPath..." -ForegroundColor Cyan

# 1. File SHA256
$installerSha256 = (Get-FileHash -Path $MsixPath -Algorithm SHA256).Hash

# 2. Signature SHA256 (AppxSignature.p7x inside the MSIX zip)
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($MsixPath)
$sigEntry = $zip.GetEntry("AppxSignature.p7x")
if (-not $sigEntry) {
    $zip.Dispose()
    throw "AppxSignature.p7x not found in MSIX. The MSIX must be signed."
}
$stream = $sigEntry.Open()
$sha = [System.Security.Cryptography.SHA256]::Create()
$signatureSha256 = [BitConverter]::ToString($sha.ComputeHash($stream)).Replace("-", "")
$stream.Close()
$zip.Dispose()

Write-Host "  InstallerSha256: $installerSha256" -ForegroundColor Green
Write-Host "  SignatureSha256: $signatureSha256" -ForegroundColor Green

# 3. Update Installer Manifest
$installerYamlPath = Join-Path $WingetDir "manuelcontrera.TextSnippets-CommandPalette.installer.yaml"
$content = Get-Content $installerYamlPath -Raw
$content = $content -replace '(?<=InstallerSha256:\s*)[0-9A-Fa-f]+', $installerSha256
$content = $content -replace '(?<=SignatureSha256:\s*)[0-9A-Fa-f]+', $signatureSha256
$content = $content -replace '(?<=PackageVersion:\s*)[0-9\.]+', $Version
Set-Content -Path $installerYamlPath -Value $content

# 4. Update Version and Locale manifests
$versionYamlPath = Join-Path $WingetDir "manuelcontrera.TextSnippets-CommandPalette.yaml"
(Get-Content $versionYamlPath -Raw) -replace '(?<=PackageVersion:\s*)[0-9\.]+', $Version | Set-Content -Path $versionYamlPath

$localeYamlPath = Join-Path $WingetDir "manuelcontrera.TextSnippets-CommandPalette.locale.en-US.yaml"
(Get-Content $localeYamlPath -Raw) -replace '(?<=PackageVersion:\s*)[0-9\.]+', $Version | Set-Content -Path $localeYamlPath

Write-Host "`nValidating manifest..." -ForegroundColor Cyan
winget validate --manifest "$WingetDir"
Write-Host "[OK] Winget manifests updated and validated successfully!" -ForegroundColor Green
