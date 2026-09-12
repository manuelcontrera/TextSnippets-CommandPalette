using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Wpf.Ui.Appearance;
using WpfApp = System.Windows.Application;

namespace SnippetsCmdPal.UI;

/// <summary>
/// Helpers to run WPF windows on a proper STA thread with a message pump,
/// and to steal foreground focus from any context (including COM MTA threads).
/// </summary>
public static class WpfAppHelper
{
    // ── P/Invoke ──────────────────────────────────────────────────────────────

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);

    // ASFW_ANY (-1) lets *any* process steal the foreground once.
    private const int ASFW_ANY = -1;

    // ── STA dispatcher thread ─────────────────────────────────────────────────

    private static Dispatcher? _staDispatcher;
    private static readonly object _lock = new();

    /// <summary>
    /// Ensures a long-lived STA dispatcher thread (with a proper WPF message
    /// pump) is running, then invokes <paramref name="action"/> on it.
    /// </summary>
    public static void RunOnStaThread(Action action)
    {
        // Fast path: if CmdPal itself is running on an STA WPF thread, use it.
        if (WpfApp.Current != null
            && WpfApp.Current.Dispatcher != null
            && !WpfApp.Current.Dispatcher.HasShutdownStarted)
        {
            WpfApp.Current.Dispatcher.Invoke(() =>
            {
                InitializeTheme();
                action();
            });
            return;
        }

        // Ensure the shared STA dispatcher thread exists.
        lock (_lock)
        {
            if (_staDispatcher == null)
            {
                var ready = new ManualResetEventSlim(false);
                var thread = new Thread(() =>
                {
                    // Create a WPF Application so WPF internals (resources,
                    // imaging, etc.) are initialised on this STA thread.
                    var app = new WpfApp { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                    _staDispatcher = Dispatcher.CurrentDispatcher;
                    ready.Set();           // unblock the caller
                    Dispatcher.Run();      // ← message pump — never returns
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Name = "SnippetsUI-STA";
                thread.Start();
                ready.Wait();            // wait until Dispatcher is ready
            }
        }

        _staDispatcher!.Invoke(() =>
        {
            InitializeTheme();
            action();
        });
    }

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    // ── Fluent theme ──────────────────────────────────────────────────────────

    public static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var val = key?.GetValue("AppsUseLightTheme");
            if (val is int intVal) return intVal == 0;
        }
        catch { }
        return false;
    }

    public static void ApplyCurrentTheme()
    {
        try
        {
            EnsureResourcesMerged();
            bool isDark = IsSystemDarkTheme();
            var target = isDark ? ApplicationTheme.Dark : ApplicationTheme.Light;
            ApplicationThemeManager.Apply(target);
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\PowerToys\CmdPal\Snippets\crash.log"),
                $"[{DateTime.Now:u}] ApplyCurrentTheme error: {ex}\n");
        }
    }

    private static void EnsureResourcesMerged()
    {
        if (WpfApp.Current != null)
        {
            var uri = new Uri("pack://application:,,,/Wpf.Ui;component/Resources/Wpf.Ui.xaml",
                              UriKind.Absolute);
            bool exists = false;
            foreach (var md in WpfApp.Current.Resources.MergedDictionaries)
            {
                if (md.Source == uri) { exists = true; break; }
            }
            if (!exists)
            {
                WpfApp.Current.Resources.MergedDictionaries.Add(
                    new ResourceDictionary { Source = uri });
            }
        }
    }

    public static void InitializeTheme()
    {
        ApplyCurrentTheme();
    }

    // ── Foreground focus ──────────────────────────────────────────────────────

    /// <summary>
    /// Brings a WPF <see cref="Window"/> to the foreground even when called
    /// from a background or COM MTA thread.
    /// Pattern: set Topmost=true before Show(), then in Loaded event remove it
    /// and call SetForegroundWindow via P/Invoke.
    /// </summary>
    public static void BringToForeground(Window win)
    {
        // Grant ourselves foreground rights before showing.
        AllowSetForegroundWindow(ASFW_ANY);

        win.Topmost = true;

        win.Loaded += (_, _) =>
        {
            // Now that the HWND exists, pull the window to the front.
            var hwnd = new WindowInteropHelper(win).Handle;

            int darkVal = IsSystemDarkTheme() ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkVal, sizeof(int));

            SetForegroundWindow(hwnd);
            win.Activate();
            win.Focus();

            // Remove Topmost so the user can switch away normally afterwards.
            win.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                (Action)(() => { win.Topmost = false; }));
        };
    }
}
