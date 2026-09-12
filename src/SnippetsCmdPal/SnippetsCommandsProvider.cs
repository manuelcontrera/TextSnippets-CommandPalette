using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using SnippetsCmdPal.Commands;
using SnippetsCmdPal.Fallbacks;
using SnippetsCmdPal.Pages;
using SnippetsCmdPal.Services;

namespace SnippetsCmdPal;

public sealed partial class SnippetsCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;
    private readonly IFallbackCommandItem[] _fallbacks;

    public SnippetsCommandsProvider()
    {
        DisplayName = Strings.ProviderTitle;
        Icon = Icons.Snippet;

        _fallbacks = [new SnippetFallbackItem()];

        var listPage = new SnippetsListPage();
        _commands =
        [
            new CommandItem(listPage)
            {
                Title = Strings.ProviderTitle,
                Subtitle = Strings.ProviderSubtitle,
                Icon = Icons.Snippet,
                MoreCommands =
                [
                    new CommandContextItem(new NewSnippetCommand()),
                    new CommandContextItem(new OpenFolderCommand()),
                    new CommandContextItem(new ToggleHookCommand())
                ]
            }
        ];
    }

    public override ICommandItem[] TopLevelCommands() => _commands;

    public override IFallbackCommandItem[] FallbackCommands() => _fallbacks;
}
