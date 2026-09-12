using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using SnippetsCmdPal.Models;

namespace SnippetsCmdPal.UI;

/// <summary>
/// Launches the SnippetEditorApp.exe (WinUI 3 / Windows App SDK) process
/// to create or edit a snippet.  Communication is done via a base64-encoded
/// JSON argument so there is no IPC socket or temp-file needed.
/// </summary>
public static class SnippetEditorLauncher
{
    /// <summary>
    /// Path to the WinUI 3 editor executable, expected to live next to this exe.
    /// </summary>
    private static string EditorExePath
    {
        get
        {
            var p1 = Path.Combine(AppContext.BaseDirectory, "SnippetEditorApp", "SnippetEditorApp.exe");
            if (File.Exists(p1)) return p1;

            var p2 = Path.Combine(AppContext.BaseDirectory, "SnippetEditorApp.exe");
            if (File.Exists(p2)) return p2;

            var p3 = @"C:\Users\manal\OneDrive\Documentos\Personal\Codigo\07 Snnipets Cmdpal\src\SnippetEditorApp\bin\Release\net10.0-windows10.0.26100.0\SnippetEditorApp.exe";
            if (File.Exists(p3)) return p3;

            return p1;
        }
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);
    private const int ASFW_ANY = -1;

    /// <summary>
    /// Opens the WinUI 3 editor for creating a new snippet or editing an
    /// existing one. Runs fire-and-forget (the process manages its own
    /// lifetime).
    /// </summary>
    public static void OpenEditor(Snippet? snippet = null)
    {
        try
        {
            string exe = EditorExePath;

            if (!File.Exists(exe))
            {
                LogError($"SnippetEditorApp.exe not found at: {exe}");
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName        = exe,
                UseShellExecute = true,   // required for packaged WinUI 3 apps
                CreateNoWindow  = false
            };

            IntPtr callerHwnd = GetForegroundWindow();
            var sb = new StringBuilder();
            if (callerHwnd != IntPtr.Zero)
            {
                sb.Append($"--parent {callerHwnd.ToInt64()} ");
            }

            if (snippet != null)
            {
                // Encode the snippet as base64 JSON so the editor can pre-fill
                var json = JsonSerializer.Serialize(snippet);
                var b64  = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
                sb.Append($"--data {b64}");
            }

            psi.Arguments = sb.ToString().Trim();

            LogError($"Starting editor: {exe} (args: {psi.Arguments})");
            AllowSetForegroundWindow(ASFW_ANY);
            var proc = Process.Start(psi);
            if (proc != null)
            {
                try { AllowSetForegroundWindow(proc.Id); } catch { }
            }
        }
        catch (Exception ex)
        {
            LogError($"OpenEditor failed: {ex}");
        }
    }

    private static void LogError(string msg)
    {
        try
        {
            var log = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "PowerToys", "CmdPal", "Snippets", "crash.log");
            File.AppendAllText(log, $"[{DateTime.Now:u}] SnippetEditorLauncher: {msg}\n");
        }
        catch { }
    }
}
