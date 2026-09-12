using Microsoft.CommandPalette.Extensions.Toolkit;
using SnippetsCmdPal.Models;
using SnippetsCmdPal.UI;

namespace SnippetsCmdPal.Commands;

public sealed partial class EditSnippetCommand : InvokableCommand
{
    private readonly Snippet _snippet;

    public EditSnippetCommand(Snippet snippet)
    {
        _snippet = snippet;
        Name = Services.Strings.CommandEditSnippet(snippet.Title);
        Icon = Icons.Edit;
    }

    public override CommandResult Invoke()
    {
        SnippetEditorLauncher.OpenEditor(_snippet);
        return CommandResult.KeepOpen();
    }
}
