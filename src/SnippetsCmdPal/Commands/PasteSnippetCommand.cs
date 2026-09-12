using System;
using System.Threading;
using Microsoft.CommandPalette.Extensions.Toolkit;
using SnippetsCmdPal.Models;
using SnippetsCmdPal.Services;

namespace SnippetsCmdPal.Commands;

public sealed partial class PasteSnippetCommand : InvokableCommand
{
    private readonly Snippet _snippet;

    public PasteSnippetCommand(Snippet snippet)
    {
        _snippet = snippet;
        Name = "Pegar en la aplicación activa";
        Icon = Icons.Paste;
    }

    public override CommandResult Invoke()
    {
        var config = SnippetManager.Instance.Config;
        var expanded = VariableExpander.Expand(_snippet.Content, config.DateFormat, config.TimeFormat);
        var targetWnd = KeyboardHookService.LastTargetWindow;

        ThreadPool.QueueUserWorkItem(_ =>
        {
            // Give Command Palette window enough time to close and restore focus (450ms)
            Thread.Sleep(450);

            if (config.PasteDirectly)
            {
                InputSimulator.PasteText(expanded, targetWnd);
            }
            else
            {
                InputSimulator.SetClipboardText(expanded);
            }
        });

        return CommandResult.Dismiss();
    }
}
