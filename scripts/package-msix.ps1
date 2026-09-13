param(
    [string]$Configuration = "Release",
    [string]$Version = "1.0.1.0",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RootDir = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$DistDir = Join-Path $RootDir "dist"
New-Item -ItemType Directory -Path $DistDir -Force | Out-Null

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  Packaging Text Snippets for WinGet / MSIX ($Version)           " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# 1. Build project if not skipped
if (-not $SkipBuild) {
    Write-Host "`n[1/4] Building projects..." -ForegroundColor Yellow
    & "$PSScriptRoot\build.ps1" -Configuration $Configuration
    if ($LASTEXITCODE -ne 0) { throw "Build failed." }
}

$TargetDir = Join-Path $RootDir "src\SnippetsCmdPal\bin\$Configuration\net10.0-windows10.0.26100.0\win-x64"
if (-not (Test-Path $TargetDir)) {
    throw "Target directory not found: $TargetDir"
}

# 2. Locate makeappx.exe and signtool.exe
Write-Host "`n[2/4] Locating Windows SDK packaging tools..." -ForegroundColor Yellow
$MakeAppx = (Get-ChildItem -Path "C:\Program Files (x86)\Windows Kits\10\bin" -Filter "makeappx.exe" -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.FullName -match 'x64' } | Select-Object -First 1).FullName
$SignTool = (Get-ChildItem -Path "C:\Program Files (x86)\Windows Kits\10\bin" -Filter "signtool.exe" -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.FullName -match 'x64' } | Select-Object -First 1).FullName

if (-not $MakeAppx) { throw "MakeAppx.exe not found in Windows Kits." }

$MsixFileName = "TextSnippets-CommandPalette-$Version-x64.msix"
$OutputMsix = Join-Path $DistDir $MsixFileName

# 3. Pack into MSIX
Write-Host "`n[3/4] Creating MSIX package: $OutputMsix" -ForegroundColor Yellow
& $MakeAppx pack /d "$TargetDir" /p "$OutputMsix" /o
if ($LASTEXITCODE -ne 0) { throw "MakeAppx pack failed." }

# 4. Sign MSIX package (required for Windows installation)
Write-Host "`n[4/4] Signing MSIX package..." -ForegroundColor Yellow
$CertSubject = "CN=SnippetsCmdPalPublisher"
$PfxPath = Join-Path $DistDir "dev_cert.pfx"
$CerPath = Join-Path $DistDir "TextSnippets.cer"

if (-not (Test-Path $PfxPath)) {
    Write-Host "  Generating self-signed certificate for $CertSubject..." -ForegroundColor Cyan
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject $CertSubject -CertStoreLocation "Cert:\CurrentUser\My" -NotAfter (Get-Date).AddYears(5)
    $password = ConvertTo-SecureString -String "snippets123!" -Force -AsPlainText
    Export-PfxCertificate -Cert $cert -FilePath $PfxPath -Password $password | Out-Null
    Export-Certificate -Cert $cert -FilePath $CerPath | Out-Null
}

if ($SignTool -and (Test-Path $PfxPath)) {
    & $SignTool sign /fd SHA256 /a /f "$PfxPath" /p "snippets123!" "$OutputMsix"
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  [OK] Package successfully signed!" -ForegroundColor Green
    } else {
        Write-Warning "SignTool exited with code $LASTEXITCODE"
    }
}

# 5. Checksums and portable zip
$hash = (Get-FileHash -Path "$OutputMsix" -Algorithm SHA256).Hash
Set-Content -Path (Join-Path $DistDir "$MsixFileName.sha256") -Value $hash

$ZipFileName = "TextSnippets-CommandPalette-$Version-x64.zip"
$OutputZip = Join-Path $DistDir $ZipFileName
Write-Host "  Creating portable ZIP: $OutputZip" -ForegroundColor Cyan
$tempZipDir = Join-Path $env:TEMP "snippets_zip_$([Guid]::NewGuid().ToString('N'))"
try {
    New-Item -ItemType Directory -Path $tempZipDir -Force | Out-Null
    Copy-Item -Path "$TargetDir\*" -Destination $tempZipDir -Recurse -Force
    Compress-Archive -Path "$tempZipDir\*" -DestinationPath "$OutputZip" -Force
} finally {
    Remove-Item -Path $tempZipDir -Recurse -Force -ErrorAction SilentlyContinue
}
$zipHash = (Get-FileHash -Path "$OutputZip" -Algorithm SHA256).Hash
Set-Content -Path (Join-Path $DistDir "$ZipFileName.sha256") -Value $zipHash

# Generate SHA256SUMS.txt
$sumsContent = @"
$hash  $MsixFileName
$zipHash  $ZipFileName
"@
Set-Content -Path (Join-Path $DistDir "SHA256SUMS.txt") -Value $sumsContent

Write-Host "`n=================================================================" -ForegroundColor Green
Write-Host "  Artifacts successfully generated in: dist/                     " -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Green
Write-Host "  - MSIX: $MsixFileName (SHA256: $hash)"
Write-Host "  - ZIP:  $ZipFileName"
Write-Host "  - CER:  TextSnippets.cer"
