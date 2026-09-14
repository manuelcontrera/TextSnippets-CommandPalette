# WinGet Package for Text Snippets

This folder contains the Windows Package Manager (`winget`) manifests for **Text Snippets for Command Palette** (`manuelcontrera.TextSnippets-CommandPalette`).

## 📦 How to Install via WinGet (Once published to winget-pkgs)

```powershell
winget install manuelcontrera.TextSnippets-CommandPalette
```

---

## 🚀 How to Publish / Update in `microsoft/winget-pkgs`

### Option 1: Automatic via `wingetcreate` (Recommended)

When a new GitHub Release is published, run:

```powershell
wingetcreate new https://github.com/manuelcontrera/TextSnippets-CommandPalette/releases/download/v1.0.1/TextSnippets-CommandPalette-1.0.1.0-x64.msix
```

`wingetcreate` will download the MSIX, extract the SHA256 and SignatureSha256 automatically, prompt you to verify the metadata, and can submit the Pull Request directly to [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs) using your GitHub token!

### Option 2: Using the pre-generated manifests in this directory

1. Run the helper script to update hashes from your built MSIX:
   ```powershell
   pwsh scripts/update-winget-manifest.ps1 -Version 1.0.1
   ```
2. Fork [microsoft/winget-pkgs](https://github.com/microsoft/winget-pkgs).
3. Copy these files to:
   `manifests/m/manuelcontrera/TextSnippets-CommandPalette/1.0.1/`
4. Submit a Pull Request to `microsoft/winget-pkgs`.

---

---

## 🧪 Validating Manifests Locally

You can validate these manifests at any time with:

```powershell
winget validate --manifest winget/
```

---

## ⚠️ Lecciones Aprendidas de Validación en WinGet (msftbot)

1. **`SignatureSha256`:**
   En paquetes MSIX, Microsoft valida que `SignatureSha256` sea el SHA256 del archivo interno `AppxSignature.p7x` (NO el hash de la clave pública del certificado). El script `scripts/update-winget-manifest.ps1` ya extrae y calcula esto automáticamente.

2. **Versión interna del MSIX:**
   El `AppxManifest.xml` empaquetado dentro del MSIX debe coincidir exactamente con `PackageVersion` (ej. `Version="1.0.1.0"` en el MSIX para `PackageVersion: 1.0.1`). Asegúrate siempre de compilar (`scripts/build.ps1`) antes de empaquetar (`scripts/package-msix.ps1`).

3. **Plataformas (`Platform`):**
   Si el MSIX define `TargetDeviceFamily` para `Windows.Universal` y `Windows.Desktop`, el manifiesto debe declarar ambas en `Platform:`.

