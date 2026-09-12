using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.XamlTypeInfo;

namespace SnippetEditorApp;

/// <summary>
/// WinUI 3 Application — Native Windows 11 Fluent Design.
/// Implements IXamlMetadataProvider using the built-in XamlControlsXamlMetaDataProvider
/// so that Controls (TextBox, Button, RadioButton, etc.) can resolve their default templates.
/// </summary>
public class App : Application, IXamlMetadataProvider
{
    private readonly XamlControlsXamlMetaDataProvider _provider = new();
    private MainWindow? _window;

    public App()
    {
        this.UnhandledException += (s, e) =>
        {
            Log("App.UnhandledException: " + e.Message + " | " + e.Exception);
            e.Handled = true;
        };
    }

    public IXamlType GetXamlType(Type type) => _provider.GetXamlType(type);
    public IXamlType GetXamlType(string fullName) => _provider.GetXamlType(fullName);
    public XmlnsDefinition[] GetXmlnsDefinitions() => _provider.GetXmlnsDefinitions();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            Log("Creating MainWindow instance");
            _window = new MainWindow();
            Log("Calling InitializeWindow");
            _window.InitializeWindow();
            Log("Activating MainWindow");
            _window.Activate();
            Log("Forcing Foreground on MainWindow");
            _window.ForceForeground();
            Log("MainWindow activated successfully!");
        }
        catch (Exception ex)
        {
            Log("OnLaunched error: " + ex);
            throw;
        }
    }

    public static void Log(string msg)
    {
        try
        {
            var log = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft", "PowerToys", "CmdPal", "Snippets", "crash.log");
            File.AppendAllText(log, $"[{DateTime.Now:u}] [WinUI3App] {msg}\n");
        }
        catch { }
    }
}
