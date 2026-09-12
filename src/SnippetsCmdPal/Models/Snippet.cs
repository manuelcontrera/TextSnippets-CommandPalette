namespace SnippetsCmdPal.Models;

public sealed class Snippet
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Trigger { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = [];
    public List<string> Options { get; set; } = [];

    public bool HasMultipleOptions => Options.Count > 1;

    public IReadOnlyList<string> GetAllOptions()
    {
        if (Options.Count > 0)
        {
            return Options;
        }

        if (!string.IsNullOrEmpty(Content))
        {
            return [Content];
        }

        return [];
    }
}
