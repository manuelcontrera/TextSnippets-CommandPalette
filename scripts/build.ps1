param(
    [string]$Configuration = "Release"
)

$PSScriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Definition
$RootDir = Join-Path $PSScriptRoot ".."
$CmdPalProj = Join-Path $RootDir "src\SnippetsCmdPal\SnippetsCmdPal.csproj"
$EditorProj = Join-Path $RootDir "src\SnippetEditorApp\SnippetEditorApp.csproj"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  Building Text Snippets ($Configuration, win-x64)              " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# Stop any running instances first
Get-Process SnippetsCmdPal, SnippetEditorApp -ErrorAction SilentlyContinue | Stop-Process -Force

# 1. Build WinUI 3 Editor
Write-Host "Compiling SnippetEditorApp..." -ForegroundColor Yellow
dotnet build "$EditorProj" -c $Configuration -r win-x64
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] SnippetEditorApp build failed." -ForegroundColor Red
    exit 1
}

# 2. Build CmdPal Extension
Write-Host "Compiling SnippetsCmdPal..." -ForegroundColor Yellow
dotnet build "$CmdPalProj" -c $Configuration -r win-x64
if ($LASTEXITCODE -ne 0) {
    Write-Host "[ERROR] SnippetsCmdPal build failed." -ForegroundColor Red
    exit 1
}

# 3. Bundle Assets and Editor App into Package folder
$TargetDir = Join-Path $RootDir "src\SnippetsCmdPal\bin\$Configuration\net10.0-windows10.0.26100.0\win-x64"
$EditorBin = Join-Path $RootDir "src\SnippetEditorApp\bin\$Configuration\net10.0-windows10.0.26100.0"
$AssetsSrc = Join-Path $RootDir "src\SnippetsCmdPal\Assets"

New-Item -ItemType Directory -Path "$TargetDir\SnippetEditorApp" -Force | Out-Null
Copy-Item -Path "$EditorBin\*" -Destination "$TargetDir\SnippetEditorApp\" -Recurse -Force
Copy-Item -Path "$EditorBin\SnippetEditorApp.*" -Destination "$TargetDir\" -Force
Copy-Item -Path "$AssetsSrc\*" -Destination "$TargetDir\Assets\" -Recurse -Force
Copy-Item -Path "$AssetsSrc\*" -Destination "$TargetDir\SnippetEditorApp\Assets\" -Recurse -Force

Write-Host "`n[OK] Build and bundling completed successfully!" -ForegroundColor Green
Write-Host "Output directory: $TargetDir" -ForegroundColor Yellow
