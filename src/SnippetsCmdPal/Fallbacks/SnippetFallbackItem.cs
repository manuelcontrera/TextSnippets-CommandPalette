using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using SnippetsCmdPal.Commands;
using SnippetsCmdPal.Models;
using SnippetsCmdPal.Services;
using SnippetsCmdPal.UI;

namespace SnippetsCmdPal.Fallbacks;

public sealed partial class SnippetFallbackItem : FallbackCommandItem
{
    private Snippet? _currentSnippet;
    private readonly DynamicPasteCommand _pasteCommand;
    private readonly DynamicCopyCommand _copyCommand;
    private readonly QuickAddSnippetCommand _quickAddCommand;

    public SnippetFallbackItem()
        : this(new DynamicPasteCommand(), new DynamicCopyCommand(), new QuickAddSnippetCommand())
    {
    }

    private SnippetFallbackItem(DynamicPasteCommand pasteCommand, DynamicCopyCommand copyCommand, QuickAddSnippetCommand quickAddCommand)
        : base(pasteCommand, "Snippet detectado", "SnippetsCmdPal.Fallback")
    {
        _pasteCommand = pasteCommand;
        _copyCommand = copyCommand;
        _quickAddCommand = quickAddCommand;
        Icon = Icons.Snippet;
        MoreCommands = [new CommandContextItem(_copyCommand)];
        Title = string.Empty;
    }

    public override void UpdateQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            Hide();
            return;
        }

        query = query.Trim();

        // 1. INLINE CREATION IN COMMAND PALETTE: "add !trigger texto" or "nuevo !trigger texto"
        var addMatch = Regex.Match(query, @"^(?:add|nuevo|!add|\+)\s+(!?[a-zA-Z0-9_\-]+)\s+(.+)$", RegexOptions.IgnoreCase);
        if (addMatch.Success)
        {
            var trigger = addMatch.Groups[1].Value;
            var content = addMatch.Groups[2].Value;

            if (!trigger.StartsWith('!'))
            {
                trigger = "!" + trigger;
            }

            Title = $"➕ Guardar nuevo snippet: {trigger}";
            Subtitle = $"Texto: \"{content}\" — Presiona Enter para guardar en Command Palette";
            Icon = Icons.Add;

            _quickAddCommand.Trigger = trigger;
            _quickAddCommand.Content = content;
            _pasteCommand.CurrentSnippet = null;
            _copyCommand.CurrentSnippet = null;

            Command = _quickAddCommand;
            MoreCommands = [];
            return;
        }

        Command = _pasteCommand;

        // 2. Exact or prefix trigger matches
        var triggerOptions = SnippetManager.Instance.GetResolvedOptionsForTrigger(query);
        if (triggerOptions.Count > 0)
        {
            if (triggerOptions.Count == 1)
            {
                var opt = triggerOptions[0];
                var s = opt.ParentSnippet;
                ShowSnippet(s, $"Disparador: {s.Trigger} (Enter para pegar)");
                MoreCommands =
                [
                    new CommandContextItem(new CopySnippetCommand(s)),
                    new CommandContextItem(new EditSnippetCommand(s))
                ];
            }
            else
            {
                var primary = triggerOptions[0];
                var primarySnippet = new Snippet
                {
                    Id = primary.ParentSnippet.Id,
                    Trigger = primary.Trigger,
                    Title = primary.Title,
                    Content = primary.Content
                };

                _currentSnippet = primarySnippet;
                Title = $"{primary.Title} (1 de {triggerOptions.Count} para {query})";
                Subtitle = $"{primary.Content} - Pulsa Enter para este o Ctrl+K para ver más opciones";
                Icon = Icons.Multiple;
                _pasteCommand.CurrentSnippet = primarySnippet;
                _copyCommand.CurrentSnippet = primarySnippet;

                var more = new List<CommandContextItem>();
                for (int i = 1; i < triggerOptions.Count; i++)
                {
                    var opt = triggerOptions[i];
                    var sub = new Snippet
                    {
                        Id = opt.ParentSnippet.Id,
                        Trigger = opt.Trigger,
                        Title = opt.Title,
                        Content = opt.Content
                    };
                    more.Add(new CommandContextItem(new PasteSnippetCommand(sub))
                    {
                        Title = $"Pegar: {opt.Title} ({opt.Content})",
                        Icon = Icons.Paste
                    });
                }
                more.Add(new CommandContextItem(new OpenPickerCommand(query, triggerOptions)));
                more.Add(new CommandContextItem(_copyCommand));
                more.Add(new CommandContextItem(new EditSnippetCommand(primary.ParentSnippet)));
                MoreCommands = more.ToArray();
            }
            return;
        }

        // 3. Search matches (if query starts with ! or contains text)
        if (query.StartsWith('!') || query.StartsWith('/') || query.StartsWith(';'))
        {
            var searchMatches = SnippetManager.Instance.Search(query);
            if (searchMatches.Count > 0)
            {
                var s = searchMatches[0];
                ShowSnippet(s, $"Coincidencia: {s.Trigger} (Enter para pegar)");
                MoreCommands =
                [
                    new CommandContextItem(new CopySnippetCommand(s)),
                    new CommandContextItem(new EditSnippetCommand(s))
                ];
                return;
            }
        }

        Hide();
    }

    private void ShowSnippet(Snippet s, string subtitle)
    {
        _currentSnippet = s;
        Title = s.Title;
        Subtitle = $"{subtitle} -> {s.Content}";
        Icon = Icons.Snippet;
        _pasteCommand.CurrentSnippet = s;
        _copyCommand.CurrentSnippet = s;
    }

    private void Hide()
    {
        _currentSnippet = null;
        Title = string.Empty;
        Subtitle = string.Empty;
        _pasteCommand.CurrentSnippet = null;
        _copyCommand.CurrentSnippet = null;
    }

    private sealed partial class QuickAddSnippetCommand : InvokableCommand
    {
        public string Trigger { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;

        public QuickAddSnippetCommand()
        {
            Name = "Guardar snippet directamente";
            Icon = Icons.Add;
        }

        public override CommandResult Invoke()
        {
            if (string.IsNullOrEmpty(Trigger) || string.IsNullOrEmpty(Content))
            {
                return CommandResult.KeepOpen();
            }

            var newSnippet = new Snippet
            {
                Trigger = Trigger,
                Title = Trigger,
                Content = Content
            };

            SnippetManager.Instance.AddOrUpdate(newSnippet);

            return CommandResult.ShowToast(new ToastArgs
            {
                Message = $"¡Snippet \"{Trigger}\" guardado correctamente!",
                Result = CommandResult.Dismiss()
            });
        }
    }

    private sealed partial class DynamicPasteCommand : InvokableCommand
    {
        public Snippet? CurrentSnippet { get; set; }

        public DynamicPasteCommand()
        {
            Name = "Pegar snippet en la aplicación activa";
            Icon = Icons.Paste;
        }

        public override CommandResult Invoke()
        {
            if (CurrentSnippet == null) return CommandResult.KeepOpen();
            var cmd = new PasteSnippetCommand(CurrentSnippet);
            return cmd.Invoke();
        }
    }

    private sealed partial class DynamicCopyCommand : InvokableCommand
    {
        public Snippet? CurrentSnippet { get; set; }

        public DynamicCopyCommand()
        {
            Name = "Copiar snippet al portapapeles";
            Icon = Icons.Copy;
        }

        public override CommandResult Invoke()
        {
            if (CurrentSnippet == null) return CommandResult.KeepOpen();
            var cmd = new CopySnippetCommand(CurrentSnippet);
            return cmd.Invoke();
        }
    }

    private sealed partial class OpenPickerCommand : InvokableCommand
    {
        private readonly string _trigger;
        private readonly IReadOnlyList<TriggerOptionItem> _options;

        public OpenPickerCommand(string trigger, IReadOnlyList<TriggerOptionItem> options)
        {
            _trigger = trigger;
            _options = options;
            Name = $"Desplegar menú selector para \"{trigger}\"...";
            Icon = Icons.Multiple;
        }

        public override CommandResult Invoke()
        {
            SnippetPickerWindow.ShowPicker(_trigger, _options, InputSimulator.GetForegroundWindow());
            return CommandResult.Dismiss();
        }
    }
}
