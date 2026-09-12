using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace SnippetsCmdPal.Services;

public static class VariableExpander
{
    private const uint CF_UNICODETEXT = 13;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetClipboardData(uint uFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    public static string GetClipboardText()
    {
        try
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                return string.Empty;
            }

            try
            {
                var handle = GetClipboardData(CF_UNICODETEXT);
                if (handle == IntPtr.Zero)
                {
                    return string.Empty;
                }

                var pointer = GlobalLock(handle);
                if (pointer == IntPtr.Zero)
                {
                    return string.Empty;
                }

                try
                {
                    return Marshal.PtrToStringUni(pointer) ?? string.Empty;
                }
                finally
                {
                    GlobalUnlock(handle);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    public static string Expand(string template, string defaultDateFormat = "yyyy-MM-dd", string defaultTimeFormat = "HH:mm")
    {
        if (string.IsNullOrEmpty(template))
        {
            return string.Empty;
        }

        var now = DateTime.Now;

        // Custom date formats: {date:FORMAT}
        var result = Regex.Replace(template, @"\{date:([^\}]+)\}", m =>
        {
            var format = m.Groups[1].Value;
            try { return now.ToString(format); } catch { return m.Value; }
        }, RegexOptions.IgnoreCase);

        // Custom time formats: {time:FORMAT}
        result = Regex.Replace(result, @"\{time:([^\}]+)\}", m =>
        {
            var format = m.Groups[1].Value;
            try { return now.ToString(format); } catch { return m.Value; }
        }, RegexOptions.IgnoreCase);

        // Standard placeholders
        result = Regex.Replace(result, @"\{date\}", now.ToString(defaultDateFormat), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{time\}", now.ToString(defaultTimeFormat), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{datetime\}", now.ToString($"{defaultDateFormat} {defaultTimeFormat}"), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{year\}", now.ToString("yyyy"), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{month\}", now.ToString("MM"), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{day\}", now.ToString("dd"), RegexOptions.IgnoreCase);
        result = Regex.Replace(result, @"\{guid\}|\{uuid\}", Guid.NewGuid().ToString(), RegexOptions.IgnoreCase);

        // Clipboard
        if (result.Contains("{clipboard}", StringComparison.OrdinalIgnoreCase))
        {
            var clip = GetClipboardText();
            result = Regex.Replace(result, @"\{clipboard\}", clip, RegexOptions.IgnoreCase);
        }

        // Clean cursor tag
        result = Regex.Replace(result, @"\{cursor\}", "", RegexOptions.IgnoreCase);

        return result;
    }
}
