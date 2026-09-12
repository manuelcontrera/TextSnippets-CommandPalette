using Microsoft.CommandPalette.Extensions.Toolkit;
using SnippetsCmdPal.Models;
using SnippetsCmdPal.Services;

namespace SnippetsCmdPal.Commands;

public sealed partial class CopySnippetCommand : InvokableCommand
{
    private readonly Snippet _snippet;

    public CopySnippetCommand(Snippet snippet)
    {
        _snippet = snippet;
        Name = Services.Strings.CommandCopySnippet(snippet.Title);
        Icon = Icons.Copy;
    }

    public override CommandResult Invoke()
    {
        var config = SnippetManager.Instance.Config;
        var expanded = VariableExpander.Expand(_snippet.Content, config.DateFormat, config.TimeFormat);
        InputSimulator.SetClipboardText(expanded);

        var toastMsg = Services.Strings.IsSpanish
            ? $"Snippet \"{_snippet.Title}\" copiado al portapapeles"
            : $"Snippet \"{_snippet.Title}\" copied to clipboard";

        return CommandResult.ShowToast(new ToastArgs
        {
            Message = toastMsg,
            Result = CommandResult.Dismiss()
        });
    }
}
