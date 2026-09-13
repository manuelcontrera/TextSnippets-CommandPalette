<p align="center">
  <img src="assets/logo.png" alt="Text Snippets Logo" width="120" height="120" />
</p>

# Text Snippets for Microsoft PowerToys Command Palette

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Windows%2011%20%7C%20Windows%2010-0078D4?logo=windows)](https://www.microsoft.com/windows)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![WinUI 3](https://img.shields.io/badge/UI-WinUI%203%20%2F%20Windows%20App%20SDK-0078D4)](https://github.com/microsoft/microsoft-ui-xaml)
[![PowerToys](https://img.shields.io/badge/PowerToys-Command%20Palette-23272A)](https://github.com/microsoft/PowerToys)

> **Raycast-style text snippets with automatic inline expansion, interactive multi-choice pickers, and a visual WinUI 3 Fluent editor for Microsoft PowerToys Command Palette.**
>
> *Snippets de texto estilo Raycast con auto-expansión en tiempo real, menú selector de opciones múltiples y editor visual Fluent WinUI 3 para Command Palette.*

---

<p align="center">
  <a href="assets/how-to-use.mp4">
    <img src="assets/demo.gif" alt="Text Snippets Demo - How to Use" width="100%" style="max-width: 850px; border-radius: 8px; box-shadow: 0 4px 16px rgba(0,0,0,0.2);" />
  </a>
</p>

<p align="center">
  <em>🎬 <strong>Demo en acción (reproducción continua).</strong> Haz clic en la animación o <a href="assets/how-to-use.mp4">aquí para abrir/descargar el video MP4 en alta resolución</a>.</em>
</p>

---

## English Overview

**Text Snippets** is a native Windows extension built for **Microsoft PowerToys Command Palette**. It brings the beloved, frictionless snippet expansion workflow from Raycast/TextExpander into the modern Windows desktop ecosystem.

### ✨ Key Features

1. **⚡ Raycast-Style Global Inline Text Expansion**:
   - Type your snippet trigger (e.g. `!email`, `!phone`, `!sig`) in **any application** (Notepad, Word, Teams, Slack, Chrome, VS Code, Terminal).
   - The low-level keyboard hook automatically erases the trigger and pastes your expanded text instantly.
   - Can be toggled on/off at any time via Command Palette.

2. **📋 Interactive Multiple-Choice Pickers**:
   - Associate multiple values with the same trigger (e.g. `!email` → Personal, Work, Support).
   - If there is 1 option, it expands immediately.
   - If there are multiple options, typing the trigger displays an interactive selection menu where you can choose using arrow keys or numeric keys (`1-9`).

3. **🎨 Visual WinUI 3 Editor (Fluent Design & Mica)**:
   - Clean, lightweight standalone editor built with **WinUI 3** and **Windows App SDK**.
   - Features Windows 11 **Mica backdrop**, dynamic dark/light mode switching, high-DPI transparent taskbar icon, and full keyboard ergonomics (`Ctrl+Enter` to save, `Esc` to cancel).
   - Includes quick-insert buttons for variables at cursor position.

4. **🧩 Deep Command Palette Integration**:
   - Search snippets by title, trigger, tags, or content inside Command Palette (`Alt + Space`).
   - Markdown detail pane with evaluated preview.
   - Commands to create, edit, copy, or delete snippets.

5. **🕒 Dynamic Template Variables**:
   - `{date}`: Current date (e.g., `2026-09-12`).
   - `{date:FORMAT}`: Custom formatted date (e.g., `{date:yyyy-MM-dd}`).
   - `{time}`: Current time (e.g., `14:30`).
   - `{datetime}`: Current date and time.
   - `{clipboard}`: Inserts current clipboard contents.
   - `{uuid}` / `{guid}`: Generates a new random GUID.
   - `{year}`, `{month}`, `{day}`: Date components.

6. **🌐 Automatic Bilingual Localization (i18n)**:
   - Seamlessly adapts to your system culture:
     - **English** by default for global users.
     - **Spanish (Español)** automatically for Spanish locales (`es`).

7. **🔒 100% Offline & Private**:
   - All snippets are stored locally on your machine in plain JSON at:
     `%LOCALAPPDATA%\Microsoft\PowerToys\CmdPal\Snippets\snippets.json`
   - Zero telemetry, zero analytics, zero network requests. Your data never leaves your computer.

---

## Visión General en Español

**Text Snippets** es una extensión nativa para **Microsoft PowerToys Command Palette** inspirada en los Snippets de Raycast. Permite tanto la búsqueda y previsualización desde la paleta de comandos como la expansión automática en tiempo real en cualquier aplicación de Windows.

### 🌟 Funcionalidades Principales
- **Expansión automática global (*Inline Text Expansion*)**: Escribe `!email`, `!tel` o cualquier atajo y el texto se expande solo sin abrir ventanas.
- **Selector múltiple inteligente**: Si un atajo tiene varias opciones, te muestra un menú rápido para elegir con números del 1 al 9 o las flechas del teclado.
- **Editor visual con Mica y Fluent Design**: Ventana moderna de WinUI 3 para crear y editar snippets con botones para insertar variables `{date}`, `{time}`, `{clipboard}`, `{uuid}`, etc.
- **Integración con Command Palette**: Busca y previsualiza snippets desde `Alt + Espacio`.
- **Privacidad total**: 100% local y offline. Ningún dato sale de tu equipo.

---

## 📂 Project Architecture

```
TextSnippets-CommandPalette/
├── Directory.Build.props              # Global MSBuild properties
├── Directory.Packages.props           # Central Package Management (CPM)
├── SnippetsCmdPal.slnx                # Modern Visual Studio Solution
├── src/
│   ├── SnippetsCmdPal/               # Command Palette Extension Project (.NET 10)
│   │   ├── SnippetsExtension.cs      # IExtension entry point
│   │   ├── SnippetsCommandsProvider.cs # Command Palette provider
│   │   ├── Package.appxmanifest      # Sparse MSIX package definition & COM server registration
│   │   ├── Services/
│   │   │   ├── Localization.cs       # Bilingual i18n engine (en / es)
│   │   │   ├── KeyboardHookService.cs# Low-level WH_KEYBOARD_LL hook for inline expansion
│   │   │   ├── SnippetManager.cs     # JSON storage & snippet lifecycle
│   │   │   ├── VariableExpander.cs   # Dynamic variable engine ({date}, {clipboard}...)
│   │   │   └── InputSimulator.cs     # SendInput key injection
│   │   ├── Pages/                    # Command Palette list & preview pages
│   │   ├── Commands/                 # Create, Edit, Copy, Delete, Toggle commands
│   │   └── Assets/                   # Fluent vector icons & multi-res ICO files
│   └── SnippetEditorApp/             # Standalone WinUI 3 Visual Editor
│       ├── Program.cs                # Entry point with WinUI 3 XamlCheck
│       ├── MainWindow.xaml.cs        # Mica window, form validation, theme watcher
│       └── Assets/                   # App icons and resources
└── scripts/
    ├── build.ps1                     # Automated build script
    ├── install-local.ps1             # Local MSIX developer registration
    └── uninstall-local.ps1           # Clean package unregistration
```

---

## 📦 Installation

### Option 1: Via Windows Package Manager (WinGet)
```powershell
winget install manuelcontrera.TextSnippets-CommandPalette
```

### Option 2: Pre-built MSIX Package
1. Download the latest `.msix` from [Releases](https://github.com/manuelcontrera/TextSnippets-CommandPalette/releases).
2. Install via WinGet locally:
   ```powershell
   winget install .\TextSnippets-CommandPalette-1.0.1.0-x64.msix
   ```
   *(Or double-click the `.msix` to install with Windows App Installer).*

---

## 🚀 Building and Running Locally

### Prerequisites
- Windows 11 (build 22621 or later recommended) or Windows 10
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- [Microsoft PowerToys](https://github.com/microsoft/PowerToys) with Command Palette enabled

### 1. Clone the Repository
```powershell
git clone https://github.com/manuelcontrera/TextSnippets-CommandPalette.git
cd TextSnippets-CommandPalette
```

### 2. Build and Register the Extension
Run the build script from PowerShell:
```powershell
# Build both the WinUI 3 editor and the Command Palette COM extension
dotnet build "src\SnippetEditorApp\SnippetEditorApp.csproj" -c Release -r win-x64
dotnet build "src\SnippetsCmdPal\SnippetsCmdPal.csproj" -c Release -r win-x64

# Copy editor binaries and assets into package directory
Copy-Item -Path "src\SnippetEditorApp\bin\Release\net10.0-windows10.0.26100.0\*" -Destination "src\SnippetsCmdPal\bin\Release\net10.0-windows10.0.26100.0\win-x64\SnippetEditorApp\" -Recurse -Force
Copy-Item -Path "src\SnippetEditorApp\bin\Release\net10.0-windows10.0.26100.0\SnippetEditorApp.*" -Destination "src\SnippetsCmdPal\bin\Release\net10.0-windows10.0.26100.0\win-x64\" -Force
Copy-Item -Path "src\SnippetsCmdPal\Assets\*" -Destination "src\SnippetsCmdPal\bin\Release\net10.0-windows10.0.26100.0\win-x64\Assets\" -Recurse -Force
Copy-Item -Path "src\SnippetsCmdPal\Assets\*" -Destination "src\SnippetsCmdPal\bin\Release\net10.0-windows10.0.26100.0\win-x64\SnippetEditorApp\Assets\" -Recurse -Force

# Register package locally
Add-AppxPackage -Register "src\SnippetsCmdPal\bin\Release\net10.0-windows10.0.26100.0\win-x64\AppxManifest.xml" -ForceApplicationShutdown
```

### 3. Open Command Palette
Press `Alt + Space` (or your configured shortcut). Search for **Text Snippets** or type `!`.

---

## ⌨️ Variables & Triggers Guide

| Variable | Output Example | Description |
| :--- | :--- | :--- |
| `{date}` | `2026-09-12` | Current date in ISO format |
| `{date:dd/MM/yyyy}` | `12/09/2026` | Current date with custom format |
| `{time}` | `17:45` | Current time (hours and minutes) |
| `{datetime}` | `2026-09-12 17:45:00` | Full date and time |
| `{clipboard}` | `[Copied text]` | Inserts current text from clipboard |
| `{uuid}` / `{guid}` | `c9a646d3-9c61-4cc9-bcaf-b223067f965b` | New random unique identifier |
| `{year}` | `2026` | Current 4-digit year |
| `{month}` | `09` | Current 2-digit month |
| `{day}` | `12` | Current 2-digit day of the month |

---

## 🔒 Privacy & Security

This extension does not connect to the internet.
- **No Analytics / Telemetry**: No tracking, usage metrics, or error telemetry are collected.
- **Local Storage Only**: Snippets and user settings are kept exclusively on local disk.
- **Open Source**: Complete C# and XAML codebase is publicly auditable.

---

## 📄 License

This project is licensed under the [MIT License](LICENSE) - see the LICENSE file for details.

Developed with ❤️ by **[manuelcontrera](https://github.com/manuelcontrera)**.
