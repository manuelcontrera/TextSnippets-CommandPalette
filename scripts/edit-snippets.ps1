$path = "$env:LOCALAPPDATA\Microsoft\PowerToys\CmdPal\Snippets\snippets.json"
if (-not (Test-Path $path)) {
    Write-Host "[INFO] Inicializando archivo de snippets..." -ForegroundColor Yellow
    $bin = Join-Path $PSScriptRoot "..\src\SnippetsCmdPal\bin\Debug\net10.0-windows10.0.26100.0\win-x64\SnippetsCmdPal.exe"
    if (Test-Path $bin) {
        Start-Process $bin -Wait
    }
}

if (Test-Path $path) {
    Write-Host "Abriendo $path" -ForegroundColor Green
    Start-Process notepad.exe $path
} else {
    Write-Host "No se encontro el archivo $path" -ForegroundColor Red
}
