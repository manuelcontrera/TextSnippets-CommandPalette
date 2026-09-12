using Microsoft.CommandPalette.Extensions.Toolkit;
using SnippetsCmdPal.UI;

namespace SnippetsCmdPal.Commands;

public sealed partial class NewSnippetCommand : InvokableCommand
{
    public NewSnippetCommand()
    {
        Name = Services.Strings.CommandNewSnippet;
        Icon = Icons.Add;
    }

    public override CommandResult Invoke()
    {
        SnippetEditorLauncher.OpenEditor();
        return CommandResult.KeepOpen();
    }
}
