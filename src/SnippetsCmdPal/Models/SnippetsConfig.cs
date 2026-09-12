namespace SnippetsCmdPal.Models;

public sealed class SnippetsConfig
{
    public bool AutoExpandEnabled { get; set; } = true;
    public bool PasteDirectly { get; set; } = true;
    public int BackspaceDelayMs { get; set; } = 5;
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public string TimeFormat { get; set; } = "HH:mm";
}
