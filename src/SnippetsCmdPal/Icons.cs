using Microsoft.CommandPalette.Extensions.Toolkit;

namespace SnippetsCmdPal;

internal static class Icons
{
    private static IconInfo? _snippetIcon;
    public static IconInfo Snippet
    {
        get
        {
            if (_snippetIcon != null) return _snippetIcon;
            try
            {
                var fullPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Square44x44Logo.png");
                if (System.IO.File.Exists(fullPath))
                {
                    try
                    {
                        var file = Windows.Storage.StorageFile.GetFileFromPathAsync(fullPath).AsTask().GetAwaiter().GetResult();
                        var streamRef = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromFile(file);
                        _snippetIcon = new IconInfo(new IconData(streamRef));
                        return _snippetIcon;
                    }
                    catch { }

                    var uri = new Uri(fullPath);
                    var streamRef2 = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromUri(uri);
                    _snippetIcon = new IconInfo(new IconData(streamRef2));
                    return _snippetIcon;
                }
            }
            catch { }

            try
            {
                var uri = new Uri("ms-appx:///Assets/Square44x44Logo.png");
                var streamRef = Windows.Storage.Streams.RandomAccessStreamReference.CreateFromUri(uri);
                _snippetIcon = new IconInfo(new IconData(streamRef));
                return _snippetIcon;
            }
            catch { }

            return new IconInfo("\uE70B");
        }
    }
    public static IconInfo Copy => new("\uE8C8");
    public static IconInfo Paste => new("\uE77F");
    public static IconInfo Add => new("\uE710");
    public static IconInfo Delete => new("\uE74D");
    public static IconInfo Edit => new("\uE70F");
    public static IconInfo FolderOpen => new("\uE838");
    public static IconInfo Settings => new("\uE713");
    public static IconInfo Lightning => new("\uE945");
    public static IconInfo Multiple => new("\uE8EC");
}
