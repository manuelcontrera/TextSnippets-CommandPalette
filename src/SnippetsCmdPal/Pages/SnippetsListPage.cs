using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using SnippetsCmdPal.Commands;
using SnippetsCmdPal.Models;
using SnippetsCmdPal.Services;

namespace SnippetsCmdPal.Pages;

public sealed partial class SnippetsListPage : DynamicListPage
{
    private string _query = string.Empty;

    public SnippetsListPage()
    {
        Icon = Icons.Snippet;
        Title = Strings.PageTitle;
        Name = "Snippets";
        PlaceholderText = Strings.PagePlaceholder;
        ShowDetails = true;

        SnippetManager.Instance.OnChanged += () =>
        {
            RaiseItemsChanged();
        };
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        _query = newSearch;
        RaiseItemsChanged();
    }

    public override IListItem[] GetItems()
    {
        var snippets = SnippetManager.Instance.Search(_query);
        var config = SnippetManager.Instance.Config;
        var resultList = new List<IListItem>();

        // Always provide the "Create New Snippet" action at the top when not strictly filtering
        if (string.IsNullOrWhiteSpace(_query) || "nuevo snippet".Contains(_query, StringComparison.OrdinalIgnoreCase) || "crear".Contains(_query, StringComparison.OrdinalIgnoreCase) || "add".Contains(_query, StringComparison.OrdinalIgnoreCase) || "new".Contains(_query, StringComparison.OrdinalIgnoreCase))
        {
            var newItem = new ListItem(new NewSnippetCommand())
            {
                Title = Strings.CreateNewSnippet,
                Subtitle = Strings.CreateNewSnippetSubtitle,
                Icon = Icons.Add,
                Details = new Details
                {
                    Title = Strings.CreateNewSnippetDetailsTitle,
                    Body = Strings.CreateNewSnippetDetailsBody
                }
            };
            resultList.Add(newItem);
        }

        foreach (var s in snippets)
        {
            var preview = VariableExpander.Expand(s.Content, config.DateFormat, config.TimeFormat);
            var tagsText = s.Tags.Count > 0 ? $" | {Strings.TagsLabel}: {string.Join(", ", s.Tags)}" : string.Empty;

            string bodyContent;
            string subtitleText;
            var moreList = new List<CommandContextItem>
            {
                new CommandContextItem(new EditSnippetCommand(s)),
                new CommandContextItem(new CopySnippetCommand(s))
            };

            if (s.Options.Count > 1)
            {
                subtitleText = $"{s.Trigger} -> [{Strings.OptionsCount(s.Options.Count)}]";
                var optionsMd = string.Join("\n", s.Options.Select((o, i) => $"{i + 1}. `{o}`"));
                bodyContent = $"### {Strings.TriggerHeading}\n`{s.Trigger}`\n\n### {Strings.OptionsHeading} ({s.Options.Count})\n{optionsMd}\n\n{Strings.OptionsHint(s.Trigger)}";

                for (int i = 0; i < s.Options.Count; i++)
                {
                    var optVal = s.Options[i];
                    var subSnippet = new Snippet
                    {
                        Id = s.Id,
                        Trigger = s.Trigger,
                        Title = $"{s.Title} (#{i + 1})",
                        Content = optVal
                    };
                    moreList.Add(new CommandContextItem(new PasteSnippetCommand(subSnippet))
                    {
                        Title = Strings.PasteOption(i + 1, optVal),
                        Icon = Icons.Paste
                    });
                }
            }
            else
            {
                subtitleText = $"{s.Trigger} -> {s.Content}";
                bodyContent = $"### {Strings.TriggerHeading}\n`{s.Trigger}`\n\n### {Strings.ContentHeading}\n```text\n{s.Content}\n```\n\n### {Strings.PreviewHeading}\n```text\n{preview}\n```{tagsText}\n\n{Strings.SingleHint}";
            }

            moreList.Add(new CommandContextItem(new DeleteSnippetCommand(s)));
            moreList.Add(new CommandContextItem(new NewSnippetCommand()));
            moreList.Add(new CommandContextItem(new OpenFolderCommand()));
            moreList.Add(new CommandContextItem(new ToggleHookCommand()));

            var details = new Details
            {
                Title = s.Title,
                Body = bodyContent
            };

            var item = new ListItem(new PasteSnippetCommand(s))
            {
                Title = s.Title,
                Subtitle = subtitleText,
                Icon = s.Options.Count > 1 ? Icons.Multiple : Icons.Snippet,
                Details = details,
                MoreCommands = moreList.ToArray()
            };
            resultList.Add(item);
        }

        if (resultList.Count == 0)
        {
            var noItem = new ListItem(new NewSnippetCommand())
            {
                Title = "No se encontraron snippets coincidentes",
                Subtitle = "Pulsa Enter para crear un nuevo snippet con este disparador",
                Icon = Icons.Add
            };
            return [noItem];
        }

        return resultList.ToArray();
    }
}
