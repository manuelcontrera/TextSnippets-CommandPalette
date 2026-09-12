using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Graphics;
using SnippetsCmdPal.Services;

namespace SnippetEditorApp;

// ─── JSON models ──────────────────────────────────────────────────────────────

public class Snippet
{
    public string Id          { get; set; } = Guid.NewGuid().ToString("N");
    public string Title       { get; set; } = string.Empty;
    public string Trigger     { get; set; } = string.Empty;
    public string Content     { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags  { get; set; } = [];
    public List<string> Options { get; set; } = [];
}

// ─── MainWindow (code-only, no XAML required) ────────────────────────────────

public sealed class MainWindow : Window
{
    private static readonly string SnippetsPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Microsoft", "PowerToys", "CmdPal", "Snippets", "snippets.json");

    private Snippet _snippet = null!;
    private bool _isEditing;
    private bool _isDialogOpen;
    private IntPtr _parentHwnd = IntPtr.Zero;

    private Grid _root = null!;
    private TextBlock _lblTitle = null!;
    private TextBlock _subTb = null!;
    private TextBox _txtTitle = null!, _txtTrigger = null!, _txtTags = null!;
    private TextBox _txtContent = null!, _txtOptions = null!;
    private RadioButton _radioSingle = null!, _radioMultiple = null!;
    private StackPanel _panelSingle = null!, _panelMultiple = null!;
    private Border _card = null!;
    private Rectangle _divider = null!;
    private Border _tipCard = null!;
    private Border _errorBorder = null!;
    private TextBlock _lblError = null!;
    private Grid _footer = null!;
    private TextBlock _hintTb = null!;
    private Button _btnDelete = null!, _btnSave = null!;
    private Windows.UI.ViewManagement.UISettings? _uiSettings;

    public MainWindow()
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 1; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--parent", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (long.TryParse(args[i + 1], out var h))
                {
                    _parentHwnd = new IntPtr(h);
                }
                i++;
            }
            else if (string.Equals(args[i], "--data", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                LoadSnippetFromBase64(args[i + 1]);
                i++;
            }
            else if (!args[i].StartsWith("--", StringComparison.Ordinal))
            {
                LoadSnippetFromBase64(args[i]);
            }
        }

        _snippet ??= new Snippet();
    }

    private void LoadSnippetFromBase64(string b64)
    {
        try
        {
            var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(b64));
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _snippet = JsonSerializer.Deserialize<Snippet>(json, options) ?? new Snippet();
            _isEditing = true;
        }
        catch (Exception ex)
        {
            App.Log("LoadSnippetFromBase64 error: " + ex.Message);
        }
    }

    public void InitializeWindow()
    {
        try { App.Log("Calling BuildUI"); BuildUI(); App.Log("BuildUI done"); } catch (Exception ex) { App.Log("BuildUI error: " + ex); throw; }
        try { App.Log("Calling PopulateForm"); PopulateForm(); App.Log("PopulateForm done"); } catch (Exception ex) { App.Log("PopulateForm error: " + ex); throw; }
        try { App.Log("Calling SetupWindow"); SetupWindow(); App.Log("SetupWindow done"); } catch (Exception ex) { App.Log("SetupWindow error: " + ex); throw; }
        try { App.Log("Calling ApplyTheme"); ApplyTheme(); App.Log("ApplyTheme done"); } catch (Exception ex) { App.Log("ApplyTheme error: " + ex); throw; }
        try { SetupThemeWatcher(); } catch { }
    }

    // ── UI construction ───────────────────────────────────────────────────────

    private void BuildUI()
    {
        _root = new Grid();
        _root.Background = null;
        _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Key preview handler on root to catch Ctrl+Enter and Esc
        _root.PreviewKeyDown += OnRootPreviewKeyDown;

        // Header
        var header = new StackPanel { Padding = new Thickness(24, 20, 24, 12), Spacing = 4 };
        _lblTitle = new TextBlock
        {
            Text  = Strings.EditorTitle(_isEditing),
            FontSize = 22,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            Style = GetResource<Style>("TitleTextBlockStyle")
        };
        header.Children.Add(_lblTitle);
        _subTb = new TextBlock
        {
            Text       = Strings.EditorSubtitle,
            FontSize   = 12,
            Style      = GetResource<Style>("CaptionTextBlockStyle")
        };
        header.Children.Add(_subTb);
        Grid.SetRow(header, 0);
        _root.Children.Add(header);

        // Scrollable form
        var form = new StackPanel { Spacing = 18, Padding = new Thickness(0, 0, 0, 16) };

        // Error notification banner (placed at TOP of form for instant visibility)
        _errorBorder = new Border
        {
            Background      = new SolidColorBrush(Windows.UI.Color.FromArgb(40, 255, 60, 60)),
            BorderBrush     = new SolidColorBrush(Windows.UI.Color.FromArgb(120, 255, 60, 60)),
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(8),
            Padding         = new Thickness(14, 10, 14, 10),
            Margin          = new Thickness(0, 0, 0, 4),
            Visibility      = Visibility.Collapsed
        };
        _lblError = new TextBlock
        {
            FontSize     = 13,
            Foreground   = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 100, 100)),
            TextWrapping = TextWrapping.Wrap
        };
        _errorBorder.Child = _lblError;
        form.Children.Add(_errorBorder);

        form.Children.Add(Label(Strings.LabelTitle, bold: true));
        _txtTitle = new TextBox
        {
            PlaceholderText = Strings.PlaceholderTitle,
            CornerRadius    = new CornerRadius(8),
            Padding         = new Thickness(10, 8, 10, 8)
        };
        _txtTitle.TextChanged += (_, _) => _errorBorder.Visibility = Visibility.Collapsed;
        form.Children.Add(_txtTitle);

        form.Children.Add(Label(Strings.LabelTrigger, bold: true));
        form.Children.Add(Label(Strings.TipTrigger, secondary: true));
        _txtTrigger = new TextBox
        {
            PlaceholderText = "!email",
            CornerRadius    = new CornerRadius(8),
            Padding         = new Thickness(10, 8, 10, 8)
        };
        _txtTrigger.TextChanged += (_, _) => _errorBorder.Visibility = Visibility.Collapsed;
        form.Children.Add(_txtTrigger);

        form.Children.Add(Label(Strings.LabelContentType, bold: true));

        _card = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(8),
            Padding         = new Thickness(16)
        };
        var cardContent = new StackPanel { Spacing = 12 };

        _radioSingle = new RadioButton
        {
            Content     = Strings.RadioSingle,
            IsChecked   = true,
            GroupName   = "ModeGroup",
            FontWeight  = Microsoft.UI.Text.FontWeights.SemiBold
        };
        _radioSingle.Checked += OnModeChanged;
        cardContent.Children.Add(_radioSingle);

        // Single text panel
        _panelSingle = new StackPanel { Spacing = 8 };
        _panelSingle.Children.Add(Label(Strings.LabelContentToExpand, secondary: true));
        _txtContent = new TextBox
        {
            AcceptsReturn   = true,
            TextWrapping    = TextWrapping.Wrap,
            Height          = 130,
            PlaceholderText = Strings.PlaceholderContent,
            CornerRadius    = new CornerRadius(8),
            Padding         = new Thickness(10, 8, 10, 8)
        };
        _txtContent.TextChanged += (_, _) => _errorBorder.Visibility = Visibility.Collapsed;
        _panelSingle.Children.Add(_txtContent);
        _panelSingle.Children.Add(Label(Strings.LabelInsertVariable, secondary: true));

        var varRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        foreach (var v in new[] { "date", "time", "clipboard", "uuid", "datetime" })
        {
            var btn = new Button
            {
                Content      = $"[{v}]",
                Tag          = $"{{{v}}}",
                CornerRadius = new CornerRadius(6),
                Padding      = new Thickness(10, 4, 10, 4)
            };
            btn.Click += OnInsertVarClicked;
            varRow.Children.Add(btn);
        }
        _panelSingle.Children.Add(varRow);
        cardContent.Children.Add(_panelSingle);

        // Divider
        _divider = new Rectangle
        {
            Height              = 1,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        cardContent.Children.Add(_divider);

        _radioMultiple = new RadioButton
        {
            Content    = Strings.RadioMultiple,
            GroupName  = "ModeGroup",
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        _radioMultiple.Checked += OnModeChanged;
        cardContent.Children.Add(_radioMultiple);

        // Multiple options panel
        _panelMultiple = new StackPanel { Spacing = 8, Visibility = Visibility.Collapsed };
        _panelMultiple.Children.Add(Label(Strings.HelpMultiple, secondary: true));
        _txtOptions = new TextBox
        {
            AcceptsReturn   = true,
            TextWrapping    = TextWrapping.Wrap,
            Height          = 130,
            PlaceholderText = Strings.PlaceholderMultiple,
            CornerRadius    = new CornerRadius(8),
            Padding         = new Thickness(10, 8, 10, 8)
        };
        _txtOptions.TextChanged += (_, _) => _errorBorder.Visibility = Visibility.Collapsed;
        _panelMultiple.Children.Add(_txtOptions);

        _tipCard = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius    = new CornerRadius(8),
            Padding         = new Thickness(12, 8, 12, 8),
            Child           = new TextBlock
            {
                Text         = Strings.EditorTipMultiple,
                FontSize     = 12,
                TextWrapping = TextWrapping.Wrap
            }
        };
        _panelMultiple.Children.Add(_tipCard);
        cardContent.Children.Add(_panelMultiple);
        _card.Child = cardContent;
        form.Children.Add(_card);

        form.Children.Add(Label(Strings.LabelTags, bold: true));
        _txtTags = new TextBox
        {
            PlaceholderText = Strings.PlaceholderTags,
            CornerRadius    = new CornerRadius(8),
            Padding         = new Thickness(10, 8, 10, 8)
        };
        form.Children.Add(_txtTags);

        var scroll = new ScrollViewer
        {
            Background = null,
            Padding = new Thickness(24, 0, 24, 0),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = form
        };
        Grid.SetRow(scroll, 1);
        _root.Children.Add(scroll);

        // Footer
        _footer = new Grid { Padding = new Thickness(24, 12, 24, 20) };
        _footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        _footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        _footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        _btnDelete = new Button
        {
            Content      = Strings.BtnDelete,
            Visibility   = _isEditing ? Visibility.Visible : Visibility.Collapsed,
            CornerRadius = new CornerRadius(8),
            Padding      = new Thickness(14, 7, 14, 7)
        };
        _btnDelete.Click += OnDeleteClicked;
        Grid.SetColumn(_btnDelete, 0);
        _footer.Children.Add(_btnDelete);

        _hintTb = new TextBlock
        {
            Text                = Strings.KeyHint,
            FontSize            = 11,
            VerticalAlignment   = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin              = new Thickness(0, 0, 16, 0)
        };
        Grid.SetColumn(_hintTb, 1);
        _footer.Children.Add(_hintTb);

        var btnCancel = new Button
        {
            Content      = Strings.BtnCancel,
            Margin       = new Thickness(0, 0, 12, 0),
            CornerRadius = new CornerRadius(8),
            Padding      = new Thickness(14, 7, 14, 7)
        };
        btnCancel.Click += (_, _) => CancelAndReturn();
        Grid.SetColumn(btnCancel, 2);
        _footer.Children.Add(btnCancel);

        _btnSave = new Button
        {
            Content      = Strings.BtnSave(_isEditing),
            Style        = GetResource<Style>("AccentButtonStyle"),
            CornerRadius = new CornerRadius(8),
            Padding      = new Thickness(16, 7, 16, 7),
            FontWeight   = Microsoft.UI.Text.FontWeights.SemiBold
        };
        _btnSave.Click += (_, _) => SaveAndReturn();
        Grid.SetColumn(_btnSave, 3);
        _footer.Children.Add(_btnSave);

        Grid.SetRow(_footer, 2);
        _root.Children.Add(_footer);

        _root.Loaded += (s, e) =>
        {
            ForceForeground();
            DispatcherQueue.TryEnqueue(() =>
            {
                _txtTitle.Focus(FocusState.Programmatic);
                if (!string.IsNullOrEmpty(_txtTitle.Text))
                {
                    _txtTitle.SelectAll();
                }
            });

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            timer.Tick += (ts, te) =>
            {
                timer.Stop();
                ForceForeground();
            };
            timer.Start();
        };

        Content = _root;
    }

    private static T? GetResource<T>(string key) where T : class
    {
        try
        {
            if (Application.Current?.Resources == null) return null;
            if (Application.Current.Resources.TryGetValue(key, out var val) && val is T res)
                return res;

            foreach (var md in Application.Current.Resources.MergedDictionaries)
            {
                if (md.TryGetValue(key, out var mdVal) && mdVal is T mdRes)
                    return mdRes;
            }

            var themeName = Application.Current.RequestedTheme == ApplicationTheme.Dark ? "Dark" : "Light";
            if (Application.Current.Resources.ThemeDictionaries.TryGetValue(themeName, out var tdObj) &&
                tdObj is ResourceDictionary td && td.TryGetValue(key, out var tdVal) && tdVal is T tdRes)
                return tdRes;

            if (Application.Current.Resources.ThemeDictionaries.TryGetValue("Default", out var defObj) &&
                defObj is ResourceDictionary defTd && defTd.TryGetValue(key, out var defVal) && defVal is T defRes)
                return defRes;
        }
        catch { }
        return null;
    }

    private static TextBlock Label(string text, bool bold = false, bool secondary = false, bool tertiary = false)
    {
        var tb = new TextBlock { Text = text };
        if (bold)
        {
            tb.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
            tb.FontSize = 13;
        }
        else
        {
            tb.FontSize = 12;
        }
        if (secondary || tertiary)
        {
            tb.Opacity = 0.72;
        }
        return tb;
    }

    private void PopulateForm()
    {
        _txtTitle.Text   = _snippet.Title;
        _txtTrigger.Text = _snippet.Trigger;
        _txtTags.Text    = string.Join(", ", _snippet.Tags);

        if (_snippet.Options.Count >= 1)
        {
            _radioMultiple.IsChecked  = true;
            _txtOptions.Text          = string.Join(Environment.NewLine, _snippet.Options);
            _panelSingle.Visibility   = Visibility.Collapsed;
            _panelMultiple.Visibility = Visibility.Visible;
        }
        else
        {
            _radioSingle.IsChecked = true;
            _txtContent.Text       = _snippet.Content;
        }
    }

    private void SetupWindow()
    {
        AppWindow.Resize(new SizeInt32(620, 720));
        AppWindow.Title = Strings.EditorWindowTitle(_isEditing, _snippet.Title);
        Title           = AppWindow.Title;
        ExtendsContentIntoTitleBar = true;

        var display  = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest);
        var work     = display.WorkArea;
        AppWindow.Move(new PointInt32(
            work.X + (work.Width  - AppWindow.Size.Width)  / 2,
            work.Y + (work.Height - AppWindow.Size.Height) / 2));

        SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsAlwaysOnTop = true;
        }

        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var icoPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (!System.IO.File.Exists(icoPath))
                icoPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (System.IO.File.Exists(icoPath))
            {
                AppWindow.SetIcon(icoPath);

                int cxIcon = GetSystemMetrics(SM_CXICON);
                int cyIcon = GetSystemMetrics(SM_CYICON);
                int cxSmIcon = GetSystemMetrics(SM_CXSMICON);
                int cySmIcon = GetSystemMetrics(SM_CYSMICON);

                IntPtr hSmall = LoadImage(IntPtr.Zero, icoPath, IMAGE_ICON, cxSmIcon > 0 ? cxSmIcon : 16, cySmIcon > 0 ? cySmIcon : 16, LR_LOADFROMFILE);
                IntPtr hBig   = LoadImage(IntPtr.Zero, icoPath, IMAGE_ICON, cxIcon > 0 ? cxIcon : 32, cyIcon > 0 ? cyIcon : 32, LR_LOADFROMFILE);
                if (hSmall != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL, hSmall);
                    SetClassLongPtr(hwnd, GCLP_HICONSM, hSmall);
                }
                if (hBig != IntPtr.Zero)
                {
                    SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_BIG, hBig);
                    SetClassLongPtr(hwnd, GCLP_HICON, hBig);
                }
            }
        }
        catch { }

        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }
        catch { }
    }

    public void ForceForeground()
    {
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);

            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

            IntPtr foregroundHwnd = GetForegroundWindow();
            uint foregroundThreadId = GetWindowThreadProcessId(foregroundHwnd, IntPtr.Zero);
            uint currentThreadId = GetCurrentThreadId();

            if (foregroundThreadId != currentThreadId && foregroundThreadId != 0)
            {
                AttachThreadInput(currentThreadId, foregroundThreadId, true);
                BringWindowToTop(hwnd);
                SetForegroundWindow(hwnd);
                AttachThreadInput(currentThreadId, foregroundThreadId, false);
            }
            else
            {
                BringWindowToTop(hwnd);
                SetForegroundWindow(hwnd);
            }
        }
        catch (Exception ex)
        {
            App.Log("ForceForeground error: " + ex.Message);
        }
    }

    // ── P/Invoke ──────────────────────────────────────────────────────────────

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    private const int SW_RESTORE = 9;

    [DllImport("user32.dll")]
    private static extern bool AllowSetForegroundWindow(int dwProcessId);
    private const int ASFW_ANY = -1;

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

    [DllImport("user32.dll", EntryPoint = "SetClassLongPtr", CharSet = CharSet.Auto)]
    private static extern IntPtr SetClassLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll", EntryPoint = "SetClassLong", CharSet = CharSet.Auto)]
    private static extern int SetClassLong32(IntPtr hWnd, int nIndex, int dwNewLong);

    private static IntPtr SetClassLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
    {
        if (IntPtr.Size == 8)
            return SetClassLongPtr64(hWnd, nIndex, dwNewLong);
        else
            return new IntPtr(SetClassLong32(hWnd, nIndex, dwNewLong.ToInt32()));
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);
    private const int SM_CXICON = 11;
    private const int SM_CYICON = 12;
    private const int SM_CXSMICON = 49;
    private const int SM_CYSMICON = 50;

    private const uint WM_SETICON = 0x0080;
    private const int ICON_SMALL = 0;
    private const int ICON_BIG = 1;
    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x0010;
    private const int GCLP_HICON = -14;
    private const int GCLP_HICONSM = -34;

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_SHOWWINDOW = 0x0040;

    // ── Theme Management ──────────────────────────────────────────────────────

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

    private void SetupThemeWatcher()
    {
        try
        {
            _uiSettings = new Windows.UI.ViewManagement.UISettings();
            _uiSettings.ColorValuesChanged += (s, e) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    ApplyTheme();
                });
            };
        }
        catch { }
    }

    private void ApplyTheme()
    {
        bool isDark = IsSystemDarkTheme();

        // 1. Force the visual tree theme (controls, textboxes, buttons, scrollviewer)
        _root.RequestedTheme = isDark ? ElementTheme.Dark : ElementTheme.Light;

        // 2. Instruct Windows 11 DWM to use immersive dark mode for window frame & Mica
        try
        {
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            int darkVal = isDark ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkVal, sizeof(int));
        }
        catch { }

        // 3. Set caption buttons (minimize, maximize, close) colors in titlebar
        try
        {
            if (AppWindow.TitleBar != null)
            {
                AppWindow.TitleBar.ButtonForegroundColor = isDark ? Colors.White : Colors.Black;
                AppWindow.TitleBar.ButtonHoverForegroundColor = isDark ? Colors.White : Colors.Black;
                AppWindow.TitleBar.ButtonInactiveForegroundColor = isDark ? Colors.DimGray : Colors.Gray;
                AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                AppWindow.TitleBar.ButtonHoverBackgroundColor = isDark ? Windows.UI.Color.FromArgb(40, 255, 255, 255) : Windows.UI.Color.FromArgb(40, 0, 0, 0);
                AppWindow.TitleBar.ButtonPressedBackgroundColor = isDark ? Windows.UI.Color.FromArgb(60, 255, 255, 255) : Windows.UI.Color.FromArgb(60, 0, 0, 0);
            }
        }
        catch { }

        // 4. Update translucent card and footer brushes so Mica shines through
        if (isDark)
        {
            _card.Background    = new SolidColorBrush(Windows.UI.Color.FromArgb(90, 38, 38, 38));
            _card.BorderBrush   = new SolidColorBrush(Windows.UI.Color.FromArgb(45, 255, 255, 255));
            _divider.Fill       = new SolidColorBrush(Windows.UI.Color.FromArgb(35, 255, 255, 255));
            _footer.Background  = new SolidColorBrush(Windows.UI.Color.FromArgb(130, 28, 28, 28));
            _subTb.Foreground   = new SolidColorBrush(Windows.UI.Color.FromArgb(200, 255, 255, 255));
            _hintTb.Foreground  = new SolidColorBrush(Windows.UI.Color.FromArgb(150, 255, 255, 255));
            _tipCard.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(35, 0, 120, 215));
            _tipCard.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(70, 0, 120, 215));
        }
        else
        {
            _card.Background    = new SolidColorBrush(Windows.UI.Color.FromArgb(160, 255, 255, 255));
            _card.BorderBrush   = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 0, 0, 0));
            _divider.Fill       = new SolidColorBrush(Windows.UI.Color.FromArgb(30, 0, 0, 0));
            _footer.Background  = new SolidColorBrush(Windows.UI.Color.FromArgb(160, 245, 245, 245));
            _subTb.Foreground   = new SolidColorBrush(Windows.UI.Color.FromArgb(180, 0, 0, 0));
            _hintTb.Foreground  = new SolidColorBrush(Windows.UI.Color.FromArgb(140, 0, 0, 0));
            _tipCard.Background = new SolidColorBrush(Windows.UI.Color.FromArgb(25, 0, 120, 215));
            _tipCard.BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(60, 0, 120, 215));
        }
    }

    // ── Keyboard & Navigation ─────────────────────────────────────────────────

    private void OnRootPreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (_isDialogOpen) return;

        if (e.Key == Windows.System.VirtualKey.Escape)
        {
            e.Handled = true;
            CancelAndReturn();
        }
        else if (e.Key == Windows.System.VirtualKey.Enter)
        {
            bool isCtrlDown = (GetKeyState(0x11 /* VK_CONTROL */) & 0x8000) != 0;
            if (isCtrlDown)
            {
                e.Handled = true;
                SaveAndReturn();
            }
        }
    }

    private void CancelAndReturn()
    {
        ReturnToCommandPalette();
        Close();
    }

    private void SaveAndReturn()
    {
        if (!ValidateAndSave()) return;
        ReturnToCommandPalette();
        Close();
    }

    private void ReturnToCommandPalette()
    {
        try
        {
            if (_parentHwnd != IntPtr.Zero)
            {
                AllowSetForegroundWindow(ASFW_ANY);
                ShowWindow(_parentHwnd, SW_RESTORE);
                SetForegroundWindow(_parentHwnd);
                return;
            }
        }
        catch { }

        try
        {
            Process.Start(new ProcessStartInfo("x-cmdpal:") { UseShellExecute = true });
        }
        catch { }
    }

    // ── Handlers ──────────────────────────────────────────────────────────────

    private void OnModeChanged(object sender, RoutedEventArgs e)
    {
        if (_radioMultiple?.IsChecked == true)
        { _panelSingle.Visibility = Visibility.Collapsed; _panelMultiple.Visibility = Visibility.Visible; }
        else
        { _panelSingle.Visibility = Visibility.Visible;  _panelMultiple.Visibility = Visibility.Collapsed; }
    }

    private void OnInsertVarClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string varTag)
        {
            int pos = _txtContent.SelectionStart;
            _txtContent.Text           = (_txtContent.Text ?? string.Empty).Insert(pos, varTag);
            _txtContent.SelectionStart  = pos + varTag.Length;
            _txtContent.SelectionLength = 0;
            _txtContent.Focus(FocusState.Programmatic);
        }
    }

    private async void OnDeleteClicked(object sender, RoutedEventArgs e)
    {
        _isDialogOpen = true;
        try
        {
            var dlg = new ContentDialog
            {
                Title             = Strings.DialogDeleteTitle,
                Content           = Strings.DialogDeleteContent(_snippet.Title, _snippet.Trigger),
                PrimaryButtonText = Strings.DialogDeleteConfirm,
                CloseButtonText   = Strings.DialogDeleteCancel,
                DefaultButton     = ContentDialogButton.Close,
                XamlRoot          = Content.XamlRoot
            };
            if (await dlg.ShowAsync() == ContentDialogResult.Primary)
            {
                DeleteSnippet(_snippet.Id);
                ReturnToCommandPalette();
                Close();
            }
        }
        finally
        {
            _isDialogOpen = false;
        }
    }

    private bool ValidateAndSave()
    {
        _errorBorder.Visibility = Visibility.Collapsed;
        var title   = _txtTitle.Text?.Trim()   ?? string.Empty;
        var trigger = _txtTrigger.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title))
        {
            ShowError(Strings.ErrorTitleRequired);
            _txtTitle.Focus(FocusState.Programmatic);
            return false;
        }

        if (string.IsNullOrWhiteSpace(trigger))
        {
            ShowError(Strings.ErrorTriggerRequired);
            _txtTrigger.Focus(FocusState.Programmatic);
            return false;
        }

        if (!trigger.StartsWith('!') && !trigger.StartsWith('/') && !trigger.StartsWith(';'))
        {
            trigger = "!" + trigger;
        }

        _snippet.Title   = title;
        _snippet.Trigger = trigger;
        _snippet.Tags    = (_txtTags.Text ?? string.Empty)
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToList();

        if (_radioMultiple?.IsChecked == true)
        {
            var opts = (_txtOptions.Text ?? string.Empty)
                        .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim()).Where(l => !string.IsNullOrEmpty(l)).ToList();

            if (opts.Count == 0)
            {
                ShowError(Strings.ErrorOptionsRequired);
                _txtOptions.Focus(FocusState.Programmatic);
                return false;
            }

            _snippet.Options = opts;
            _snippet.Content = opts[0];
        }
        else
        {
            var content = _txtContent.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(content))
            {
                ShowError(Strings.ErrorContentRequired);
                _txtContent.Focus(FocusState.Programmatic);
                return false;
            }

            _snippet.Content = content;
            _snippet.Options = [];
        }

        try
        {
            SaveSnippet(_snippet);
            App.Log($"Snippet '{_snippet.Title}' ({_snippet.Trigger}) saved successfully to disk.");
            return true;
        }
        catch (Exception ex)
        {
            ShowError((Strings.IsSpanish ? "Error al guardar en disco: " : "Error saving to disk: ") + ex.Message);
            App.Log("SaveSnippet error: " + ex);
            return false;
        }
    }

    private void ShowError(string msg)
    {
        _lblError.Text = "⚠️ " + msg;
        _errorBorder.Visibility = Visibility.Visible;
    }

    // ── Storage helpers ───────────────────────────────────────────────────────

    private static List<Snippet> LoadSnippets()
    {
        try
        {
            if (File.Exists(SnippetsPath))
            {
                using var fs = new FileStream(SnippetsPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<Snippet>>(reader.ReadToEnd(), options) ?? [];
            }
        }
        catch (Exception ex)
        {
            App.Log("LoadSnippets error: " + ex.Message);
        }
        return [];
    }

    private static void WriteSnippets(List<Snippet> list)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SnippetsPath)!);
        var options = new JsonSerializerOptions { WriteIndented = true };
        using var fs = new FileStream(SnippetsPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        using var writer = new StreamWriter(fs, System.Text.Encoding.UTF8);
        writer.Write(JsonSerializer.Serialize(list, options));
    }

    private static void SaveSnippet(Snippet s)
    {
        var list = LoadSnippets();
        var i = list.FindIndex(x => string.Equals(x.Id, s.Id, StringComparison.OrdinalIgnoreCase));
        if (i >= 0)
        {
            list[i] = s;
        }
        else
        {
            list.Add(s);
        }
        WriteSnippets(list);
    }

    private static void DeleteSnippet(string id)
    {
        var list = LoadSnippets();
        list.RemoveAll(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
        WriteSnippets(list);
    }
}
