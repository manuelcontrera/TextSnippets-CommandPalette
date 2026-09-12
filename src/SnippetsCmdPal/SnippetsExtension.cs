using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CommandPalette.Extensions;
using SnippetsCmdPal.Services;

namespace SnippetsCmdPal;

[Guid("7532b037-c4af-4cd0-99af-d969f670cb33")]
public sealed partial class SnippetsExtension : IExtension, IDisposable
{
    private readonly ManualResetEvent _extensionDisposedEvent;
    private readonly SnippetsCommandsProvider _commandsProvider;
    private readonly KeyboardHookService _keyboardHook;

    public SnippetsExtension(ManualResetEvent extensionDisposedEvent)
    {
        _extensionDisposedEvent = extensionDisposedEvent;
        _commandsProvider = new SnippetsCommandsProvider();

        // Start global keyboard hook in background
        _keyboardHook = new KeyboardHookService();
        _keyboardHook.Start();
    }

    public object? GetProvider(ProviderType providerType) => providerType switch
    {
        ProviderType.Commands => _commandsProvider,
        _ => null,
    };

    public void Dispose()
    {
        _keyboardHook.Dispose();
        _extensionDisposedEvent.Set();
    }
}
