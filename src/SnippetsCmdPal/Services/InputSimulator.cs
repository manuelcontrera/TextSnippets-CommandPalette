using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SnippetsCmdPal.Services;

public static class InputSimulator
{
    private const byte VK_BACK = 0x08;
    private const byte VK_CONTROL = 0x11;
    private const byte VK_V = 0x56;
    private const uint KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    public static void SendBackspaces(int count, int delayMs = 10)
    {
        if (count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            keybd_event(VK_BACK, 0, 0, UIntPtr.Zero);
            keybd_event(VK_BACK, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            if (delayMs > 0)
            {
                Thread.Sleep(delayMs);
            }
        }
    }

    public static bool SetClipboardText(string text)
    {
        bool success = false;
        var thread = new Thread(() =>
        {
            try
            {
                Clipboard.SetText(text);
                success = true;
            }
            catch
            {
                // Fallback retry
                try
                {
                    Thread.Sleep(50);
                    Clipboard.SetText(text);
                    success = true;
                }
                catch { }
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join(500);
        return success;
    }

    public static void PasteText(string text, IntPtr targetWindow = default)
    {
        if (string.IsNullOrEmpty(text)) return;

        SetClipboardText(text);
        Thread.Sleep(50);

        if (targetWindow != IntPtr.Zero && targetWindow != GetForegroundWindow())
        {
            SetForegroundWindow(targetWindow);
            Thread.Sleep(100);
        }

        // Simulate Ctrl + V
        keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
        keybd_event(VK_V, 0, 0, UIntPtr.Zero);
        keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
    }

    public static void ActivateCommandPalette()
    {
        try
        {
            Process.Start(new ProcessStartInfo("x-cmdpal:") { UseShellExecute = true });
        }
        catch { }
    }
}
