using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT;

namespace SnippetEditorApp;

public static class Program
{
    private static App? _app;

    [DllImport("shell32.dll", SetLastError = true)]
    private static extern int SetCurrentProcessExplicitAppUserModelID([MarshalAs(UnmanagedType.LPWStr)] string AppID);

    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            SetCurrentProcessExplicitAppUserModelID("SnippetsCmdPal.Extension_h8tt3yhg29vee!App");
        }
        catch { }

        App.Log("Program.Main started");

        ComWrappersSupport.InitializeComWrappers();
        Application.Start((p) =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            _app = new App();
        });
    }
}
