using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using Wpf.Ui.Controls;
using SnippetsCmdPal.Services;

namespace SnippetsCmdPal.UI;

public partial class SnippetPickerWindow : FluentWindow
{
    public sealed class PickerOptionDisplay
    {
        public string IndexNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ContentPreview { get; set; } = string.Empty;
        public TriggerOptionItem RawOption { get; set; } = null!;
    }

    private readonly string _trigger;
    private readonly IReadOnlyList<TriggerOptionItem> _allOptions;
    private readonly IntPtr _targetWindow;
    private List<PickerOptionDisplay> _currentFiltered = [];

    public SnippetPickerWindow(string trigger, IReadOnlyList<TriggerOptionItem> options, IntPtr targetWindow)
    {
        _trigger = trigger;
        _allOptions = options;
        _targetWindow = targetWindow;

        WpfAppHelper.ApplyCurrentTheme();

        InitializeComponent();

        TxtHeader.Text = $"⚡ {trigger}";
        TxtSubtitle.Text = $"Selecciona una opción para el disparador \"{trigger}\"";

        Loaded += (_, _) =>
        {
            ApplyFilter(string.Empty);
            TxtSearch.Focus();
        };

        KeyDown += OnWindowKeyDown;

        Microsoft.Win32.UserPreferenceChangedEventHandler themeHandler = (_, _) =>
        {
            Dispatcher.Invoke(() => WpfAppHelper.ApplyCurrentTheme());
        };
        Microsoft.Win32.SystemEvents.UserPreferenceChanged += themeHandler;
        Closed += (_, _) =>
        {
            Microsoft.Win32.SystemEvents.UserPreferenceChanged -= themeHandler;
        };
    }

    public static void ShowPicker(string trigger, IReadOnlyList<TriggerOptionItem> options, IntPtr targetWindow)
    {
        WpfAppHelper.RunOnStaThread(() =>
        {
            try
            {
                var win = new SnippetPickerWindow(trigger, options, targetWindow);
                WpfAppHelper.BringToForeground(win);   // must be BEFORE Show()
                win.ShowDialog();  // blocks the STA thread so the message pump stays alive
            }
            catch (Exception ex)
            {
                System.IO.File.AppendAllText(
                    System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        @"Microsoft\PowerToys\CmdPal\Snippets\crash.log"),
                    $"[{DateTime.Now:u}] ShowPicker error: {ex}\n");
            }
        });
    }

    private void ApplyFilter(string filter)
    {
        var filtered = _allOptions.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(filter))
        {
            filtered = filtered.Where(o =>
                o.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                o.Content.Contains(filter, StringComparison.OrdinalIgnoreCase));
        }

        var list = new List<PickerOptionDisplay>();
        int idx = 1;
        foreach (var opt in filtered)
        {
            var preview = opt.Content.Replace("\r", " ").Replace("\n", " ");
            if (preview.Length > 80) preview = preview.Substring(0, 80) + "...";

            list.Add(new PickerOptionDisplay
            {
                IndexNumber = idx <= 9 ? idx.ToString() : string.Empty,
                Title = opt.Title,
                Content = opt.Content,
                ContentPreview = preview,
                RawOption = opt
            });
            idx++;
        }

        _currentFiltered = list;
        LstOptions.ItemsSource = _currentFiltered;
        if (_currentFiltered.Count > 0)
        {
            LstOptions.SelectedIndex = 0;
        }
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter(TxtSearch.Text);
    }

    private void OnSearchPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Down)
        {
            if (_currentFiltered.Count > 0)
            {
                LstOptions.Focus();
                LstOptions.SelectedIndex = Math.Min(LstOptions.SelectedIndex + 1, _currentFiltered.Count - 1);
            }
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            ConfirmSelection();
            e.Handled = true;
            return;
        }

        // Quick digit selection if search box is empty
        if (string.IsNullOrEmpty(TxtSearch.Text) && e.Key >= Key.D1 && e.Key <= Key.D9)
        {
            int digit = e.Key - Key.D1; // 0-based
            if (digit < _currentFiltered.Count)
            {
                LstOptions.SelectedIndex = digit;
                ConfirmSelection();
                e.Handled = true;
                return;
            }
        }
    }

    private void OnListPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            ConfirmSelection();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Up && LstOptions.SelectedIndex <= 0)
        {
            TxtSearch.Focus();
            e.Handled = true;
            return;
        }

        if (e.Key >= Key.D1 && e.Key <= Key.D9)
        {
            int digit = e.Key - Key.D1;
            if (digit < _currentFiltered.Count)
            {
                LstOptions.SelectedIndex = digit;
                ConfirmSelection();
                e.Handled = true;
                return;
            }
        }
    }

    private void OnWindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void OnListDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ConfirmSelection();
    }

    private void OnCloseClicked(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ConfirmSelection()
    {
        if (LstOptions.SelectedItem is not PickerOptionDisplay selected)
        {
            if (_currentFiltered.Count > 0)
            {
                selected = _currentFiltered[0];
            }
            else
            {
                Close();
                return;
            }
        }

        var textToPaste = selected.Content;
        var target = _targetWindow;

        Close();

        ThreadPool.QueueUserWorkItem(_ =>
        {
            Thread.Sleep(80);
            var config = SnippetManager.Instance.Config;
            var expanded = VariableExpander.Expand(textToPaste, config.DateFormat, config.TimeFormat);
            if (config.PasteDirectly)
            {
                InputSimulator.PasteText(expanded, target);
            }
            else
            {
                InputSimulator.SetClipboardText(expanded);
            }
        });
    }
}
