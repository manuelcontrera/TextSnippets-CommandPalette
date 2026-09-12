using Microsoft.CommandPalette.Extensions.Toolkit;
using SnippetsCmdPal.Services;

namespace SnippetsCmdPal.Commands;

public sealed partial class ToggleHookCommand : InvokableCommand
{
    public ToggleHookCommand()
    {
        UpdateState();
    }

    public void UpdateState()
    {
        var enabled = SnippetManager.Instance.Config.AutoExpandEnabled;
        Name = Services.Strings.CommandToggleHook(enabled);
        Icon = Icons.Lightning;
    }

    public override CommandResult Invoke()
    {
        var cfg = SnippetManager.Instance.Config;
        cfg.AutoExpandEnabled = !cfg.AutoExpandEnabled;
        SnippetManager.Instance.Config = cfg;
        UpdateState();

        return CommandResult.ShowToast(new ToastArgs
        {
            Message = Services.Strings.ToastHookState(cfg.AutoExpandEnabled),
            Result = CommandResult.KeepOpen()
        });
    }
}
