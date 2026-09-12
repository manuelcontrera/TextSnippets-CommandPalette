Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "  Desinstalando Text Snippets de Command Palette                 " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

$pkg = Get-AppxPackage -Name *SnippetsCmdPal*
if ($pkg) {
    Remove-AppxPackage -Package $pkg.PackageFullName
    Write-Host "[OK] Paquete desregistrado exitosamente." -ForegroundColor Green
} else {
    Write-Host "[INFO] No se encontro el paquete instalado." -ForegroundColor Yellow
}
