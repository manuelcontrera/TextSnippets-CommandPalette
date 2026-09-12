using System.Diagnostics;
using Microsoft.CommandPalette.Extensions.Toolkit;
using SnippetsCmdPal.Services;

namespace SnippetsCmdPal.Commands;

public sealed partial class OpenFolderCommand : InvokableCommand
{
    public OpenFolderCommand()
    {
        Name = Services.Strings.CommandOpenFolder;
        Icon = Icons.FolderOpen;
    }

    public override CommandResult Invoke()
    {
        try
        {
            var dir = SnippetManager.Instance.StorageDirectory;
            Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
        }
        catch { }

        return CommandResult.Dismiss();
    }
}
