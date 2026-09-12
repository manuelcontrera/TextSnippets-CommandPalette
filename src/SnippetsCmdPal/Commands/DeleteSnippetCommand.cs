using Microsoft.CommandPalette.Extensions.Toolkit;
using SnippetsCmdPal.Models;
using SnippetsCmdPal.Services;

namespace SnippetsCmdPal.Commands;

public sealed partial class DeleteSnippetCommand : InvokableCommand
{
    private readonly Snippet _snippet;

    public DeleteSnippetCommand(Snippet snippet)
    {
        _snippet = snippet;
        Name = Services.Strings.CommandDeleteSnippet(snippet.Title);
        Icon = Icons.Delete;
    }

    public override CommandResult Invoke()
    {
        SnippetManager.Instance.Delete(_snippet.Id);
        return CommandResult.ShowToast(new ToastArgs
        {
            Message = Services.Strings.ToastDeleted(_snippet.Title),
            Result = CommandResult.KeepOpen()
        });
    }
}
