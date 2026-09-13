using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using SnippetsCmdPal.Models;
using SnippetsCmdPal.UI;

namespace SnippetsCmdPal.Services;

public sealed class KeyboardHookService : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;

    private const int VK_BACK = 0x08;
    private const int VK_RETURN = 0x0D;
    private const int VK_ESCAPE = 0x1B;
    private const int VK_SPACE = 0x20;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll")]
    private static extern bool TranslateMessage([In] ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

    [DllImport("user32.dll")]
    private static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int vKey);

    [DllImport("user32.dll")]
    private static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public IntPtr hwnd;
        public uint message;
        public IntPtr wParam;
        public IntPtr lParam;
        public uint time;
        public int pt_x;
        public int pt_y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    private static LowLevelKeyboardProc? _staticProc;
    private static IntPtr _hookHandle = IntPtr.Zero;
    private static uint _hookThreadId;
    private static IntPtr _lastTargetWindow = IntPtr.Zero;

    private Thread? _hookThread;
    private readonly StringBuilder _buffer = new(64);
    private readonly object _bufferLock = new();
    private bool _isDisposed;
    private bool _isSimulating;

    public static IntPtr LastTargetWindow => _lastTargetWindow;
    public bool IsRunning => _hookHandle != IntPtr.Zero;

    public void Start()
    {
        if (IsRunning || _isDisposed) return;

        var readyEvent = new ManualResetEvent(false);

        _hookThread = new Thread(() =>
        {
            _hookThreadId = GetCurrentThreadId();
            _staticProc = HookCallback;

            _hookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _staticProc, IntPtr.Zero, 0);

            readyEvent.Set();

            if (_hookHandle != IntPtr.Zero)
            {
                while (GetMessage(out var msg, IntPtr.Zero, 0, 0))
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);
                }

                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        })
        {
            IsBackground = true,
            Name = "SnippetsKeyboardHookThread"
        };

        _hookThread.SetApartmentState(ApartmentState.STA);
        _hookThread.Start();

        readyEvent.WaitOne(2000);
    }

    public void Stop()
    {
        if (_hookThreadId != 0)
        {
            PostThreadMessage(_hookThreadId, 0x0012 /* WM_QUIT */, IntPtr.Zero, IntPtr.Zero);
            _hookThread?.Join(1000);
            _hookThread = null;
            _hookThreadId = 0;
        }

        if (_hookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
        {
            var kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

            if (_isSimulating || !SnippetManager.Instance.Config.AutoExpandEnabled)
            {
                return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
            }

            // Skip modifier keys themselves
            uint vk = kb.vkCode;
            if (vk == 0x10 || vk == 0x11 || vk == 0x12 || vk == 0x5B || vk == 0x5C || vk == 0x14 ||
                (vk >= 0xA0 && vk <= 0xA5))
            {
                return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
            }

            // Track target window
            var fg = GetForegroundWindow();
            if (fg != IntPtr.Zero)
            {
                GetWindowThreadProcessId(fg, out var pid);
                if (!IsCmdPalProcess(pid))
                {
                    _lastTargetWindow = fg;
                }
            }

            ProcessKey(kb.vkCode, kb.scanCode);
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private static bool IsCmdPalProcess(uint pid)
    {
        try
        {
            using var proc = Process.GetProcessById((int)pid);
            return proc.ProcessName.Contains("CmdPal", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private void ProcessKey(uint vkCode, uint scanCode)
    {
        lock (_bufferLock)
        {
            if (vkCode == (uint)VK_BACK)
            {
                if (_buffer.Length > 0)
                {
                    _buffer.Length--;
                }
                return;
            }

            if (vkCode == (uint)VK_RETURN || vkCode == (uint)VK_ESCAPE)
            {
                _buffer.Clear();
                return;
            }

            var ch = GetCharFromKey(vkCode, scanCode);
            if (ch == '\0') return;

            _buffer.Append(ch);
            if (_buffer.Length > 50)
            {
                _buffer.Remove(0, _buffer.Length - 50);
            }

            var currentBuffer = _buffer.ToString();

            // Match triggers
            var allSnippets = SnippetManager.Instance.GetAll();
            var matchedTrigger = allSnippets
                .Select(s => s.Trigger.Trim())
                .Where(t => !string.IsNullOrEmpty(t) &&
                           (currentBuffer.EndsWith(t, StringComparison.OrdinalIgnoreCase) ||
                            currentBuffer.EndsWith(t + " ", StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(t => t.Length)
                .FirstOrDefault();

            if (matchedTrigger != null)
            {
                bool endsWithSpace = currentBuffer.EndsWith(matchedTrigger + " ", StringComparison.OrdinalIgnoreCase);
                int backspacesToErase = matchedTrigger.Length + (endsWithSpace ? 1 : 0);

                var resolvedOptions = SnippetManager.Instance.GetResolvedOptionsForTrigger(matchedTrigger);
                if (resolvedOptions.Count == 0) return;

                _buffer.Clear();
                var targetWnd = _lastTargetWindow;

                ThreadPool.QueueUserWorkItem(_ =>
                {
                    _isSimulating = true;
                    try
                    {
                        var config = SnippetManager.Instance.Config;

                        if (resolvedOptions.Count == 1)
                        {
                            var single = resolvedOptions[0];
                            InputSimulator.SendBackspaces(backspacesToErase, config.BackspaceDelayMs);

                            var expanded = VariableExpander.Expand(single.Content, config.DateFormat, config.TimeFormat);
                            if (config.PasteDirectly)
                            {
                                InputSimulator.PasteText(expanded, targetWnd);
                            }
                            else
                            {
                                InputSimulator.SetClipboardText(expanded);
                            }
                        }
                        else
                        {
                            // Multiple options -> Erase trigger and open modern Fluent Quick Picker!
                            InputSimulator.SendBackspaces(backspacesToErase, config.BackspaceDelayMs);
                            Thread.Sleep(50);
                            SnippetPickerWindow.ShowPicker(matchedTrigger, resolvedOptions, targetWnd);
                        }
                    }
                    catch { }
                    finally
                    {
                        Thread.Sleep(100);
                        _isSimulating = false;
                    }
                });
            }
        }
    }

    private static char GetCharFromKey(uint vkCode, uint scanCode)
    {
        var keyState = new byte[256];

        // Synchronize physical modifiers with GetAsyncKeyState
        if ((GetAsyncKeyState(0x10 /* VK_SHIFT */) & 0x8000) != 0)
        {
            keyState[0x10] = 0x80;
            keyState[0xA0] = 0x80;
        }
        if ((GetAsyncKeyState(0x11 /* VK_CONTROL */) & 0x8000) != 0)
        {
            keyState[0x11] = 0x80;
            keyState[0xA2] = 0x80;
        }
        if ((GetAsyncKeyState(0x12 /* VK_MENU / ALT */) & 0x8000) != 0)
        {
            keyState[0x12] = 0x80;
            keyState[0xA4] = 0x80;
        }
        if ((GetKeyState(0x14 /* VK_CAPITAL */) & 0x0001) != 0)
        {
            keyState[0x14] = 0x01;
        }

        var foregroundWnd = GetForegroundWindow();
        var threadId = GetWindowThreadProcessId(foregroundWnd, out _);
        var layout = GetKeyboardLayout(threadId);

        var sb = new StringBuilder(4);
        // Bit 2 (0x04) prevents modifying the kernel dead-key state (Windows 10 1607+ / Windows 11),
        // avoiding double accents/tildes (´´) in layouts like Latin American or Spanish.
        var res = ToUnicodeEx(vkCode, scanCode, keyState, sb, sb.Capacity, 0x04, layout);
        if (res > 0 && sb.Length > 0)
        {
            return sb[0];
        }

        return '\0';
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        Stop();
    }
}
