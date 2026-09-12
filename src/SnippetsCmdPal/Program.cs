using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using Microsoft.CommandPalette.Extensions;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;
using SnippetsCmdPal.Services;
using SnippetsCmdPal.UI;

namespace SnippetsCmdPal;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 1. Launched as PowerToys Command Palette COM Server
        if (args.Length > 0 && args[0] == "-RegisterProcessAsComServer")
        {
            var mtaThread = new Thread(() =>
            {
                try
                {
                    using var extensionDisposedEvent = new ManualResetEvent(false);
                    var extension = new SnippetsExtension(extensionDisposedEvent);
                    var server = new ComServer();
                    server.RegisterClass<SnippetsExtension, IExtension>(() => extension);
                    server.Start();
                    extensionDisposedEvent.WaitOne();
                    server.Stop();
                    server.UnsafeDispose();
                }
                catch { }
            });
            mtaThread.SetApartmentState(ApartmentState.MTA);
            mtaThread.Start();
            mtaThread.Join();
            return;
        }

        // 2. Launched directly by the user (Background Resident Mode with System Tray)
        try
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try
                {
                    var logPath = System.IO.Path.Combine(SnippetManager.Instance.StorageDirectory, "crash.log");
                    System.IO.File.WriteAllText(logPath, e.ExceptionObject?.ToString());
                }
                catch { }
            };

            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var hook = new KeyboardHookService();
            hook.Start();

            Icon? appIcon = null;
            try
            {
                var icoPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
                if (System.IO.File.Exists(icoPath))
                {
                    appIcon = new Icon(icoPath);
                }
            }
            catch { }

            var trayIcon = new NotifyIcon
            {
                Icon = appIcon ?? SystemIcons.Information,
                Text = "Text Snippets para Command Palette (Activo)",
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();

            var mnuNew = new ToolStripMenuItem("➕ Crear nuevo snippet...", null, (_, _) =>
            {
                SnippetEditorLauncher.OpenEditor();
            });
            contextMenu.Items.Add(mnuNew);

            var mnuOpenFolder = new ToolStripMenuItem("📂 Abrir carpeta de snippets.json", null, (_, _) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo("explorer.exe", SnippetManager.Instance.StorageDirectory) { UseShellExecute = true });
                }
                catch { }
            });
            contextMenu.Items.Add(mnuOpenFolder);

            contextMenu.Items.Add(new ToolStripSeparator());

            var mnuToggle = new ToolStripMenuItem("⚡ Expansión automática activada", null, (s, _) =>
            {
                var cfg = SnippetManager.Instance.Config;
                cfg.AutoExpandEnabled = !cfg.AutoExpandEnabled;
                SnippetManager.Instance.Config = cfg;
                if (s is ToolStripMenuItem item)
                {
                    item.Text = cfg.AutoExpandEnabled ? "⚡ Expansión automática activada" : "⚡ Expansión automática pausada";
                }
            });
            contextMenu.Items.Add(mnuToggle);

            contextMenu.Items.Add(new ToolStripSeparator());

            var mnuExit = new ToolStripMenuItem("❌ Cerrar aplicación", null, (_, _) =>
            {
                trayIcon.Visible = false;
                hook.Dispose();
                Application.Exit();
            });
            contextMenu.Items.Add(mnuExit);

            trayIcon.ContextMenuStrip = contextMenu;

            trayIcon.ShowBalloonTip(
                3000,
                "Text Snippets Activo",
                "La expansión automática de snippets está activa en segundo plano (ej: teclea !email o abre Command Palette).",
                ToolTipIcon.Info
            );

            Application.Run(new ApplicationContext());
        }
        catch (Exception ex)
        {
            try
            {
                var logPath = System.IO.Path.Combine(SnippetManager.Instance.StorageDirectory, "crash.log");
                System.IO.File.WriteAllText(logPath, ex.ToString());
            }
            catch { }
        }
    }
}
