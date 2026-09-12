using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SnippetsCmdPal.Models;

namespace SnippetsCmdPal.Services;

public sealed class SnippetManager
{
    private static readonly Lazy<SnippetManager> _instance = new(() => new SnippetManager());
    public static SnippetManager Instance => _instance.Value;

    private readonly string _storageDir;
    private readonly string _snippetsFilePath;
    private readonly string _configFilePath;
    private readonly object _lock = new();

    private List<Snippet> _snippets = [];
    private SnippetsConfig _config = new();
    private FileSystemWatcher? _watcher;
    private DateTime _lastLoadedUtc = DateTime.MinValue;

    public event Action? OnChanged;

    public SnippetsConfig Config
    {
        get { lock (_lock) { return _config; } }
        set
        {
            lock (_lock) { _config = value; }
            SaveConfig();
            OnChanged?.Invoke();
        }
    }

    private SnippetManager()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _storageDir = Path.Combine(localAppData, "Microsoft", "PowerToys", "CmdPal", "Snippets");
        Directory.CreateDirectory(_storageDir);

        _snippetsFilePath = Path.Combine(_storageDir, "snippets.json");
        _configFilePath = Path.Combine(_storageDir, "config.json");

        Load();
        InitWatcher();
    }

    private void InitWatcher()
    {
        try
        {
            _watcher = new FileSystemWatcher(_storageDir, "snippets.json")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                EnableRaisingEvents = true
            };
            _watcher.Changed += OnWatcherFileChanged;
            _watcher.Created += OnWatcherFileChanged;
            _watcher.Renamed += OnWatcherFileChanged;
        }
        catch { }
    }

    private void OnWatcherFileChanged(object sender, FileSystemEventArgs e)
    {
        System.Threading.Thread.Sleep(60);
        if (EnsureUpToDate())
        {
            OnChanged?.Invoke();
        }
    }

    public bool EnsureUpToDate()
    {
        try
        {
            if (File.Exists(_snippetsFilePath))
            {
                var lastWrite = File.GetLastWriteTimeUtc(_snippetsFilePath);
                if (lastWrite > _lastLoadedUtc)
                {
                    Load();
                    return true;
                }
            }
        }
        catch { }
        return false;
    }

    public string StorageDirectory => _storageDir;
    public string SnippetsFilePath => _snippetsFilePath;

    public void Load()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    using var fs = new FileStream(_configFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fs);
                    var json = reader.ReadToEnd();
                    _config = JsonSerializer.Deserialize<SnippetsConfig>(json) ?? new SnippetsConfig();
                }
            }
            catch
            {
                _config = new SnippetsConfig();
            }

            try
            {
                if (File.Exists(_snippetsFilePath))
                {
                    using var fs = new FileStream(_snippetsFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fs);
                    var json = reader.ReadToEnd();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    _snippets = JsonSerializer.Deserialize<List<Snippet>>(json, options) ?? [];
                    _lastLoadedUtc = File.GetLastWriteTimeUtc(_snippetsFilePath);
                }
                else
                {
                    _snippets = GetDefaultSnippets();
                    SaveSnippetsInternal();
                    _lastLoadedUtc = File.Exists(_snippetsFilePath) ? File.GetLastWriteTimeUtc(_snippetsFilePath) : DateTime.UtcNow;
                }
            }
            catch
            {
                _snippets = GetDefaultSnippets();
            }
        }
    }

    private List<Snippet> GetDefaultSnippets()
    {
        if (Strings.IsSpanish)
        {
            return
            [
                new Snippet
                {
                    Id = "email-work",
                    Trigger = "!email",
                    Title = "Correo de Trabajo",
                    Content = "juan.perez@miempresa.com",
                    Description = "Email profesional corporativo",
                    Tags = ["trabajo", "email"]
                },
                new Snippet
                {
                    Id = "email-personal",
                    Trigger = "!email",
                    Title = "Correo Personal",
                    Content = "juanperez@gmail.com",
                    Description = "Email personal para comunicaciones generales",
                    Tags = ["personal", "email"]
                },
                new Snippet
                {
                    Id = "meeting-intro",
                    Trigger = "!intro",
                    Title = "Saludo de Reunión",
                    Content = "Hola a todos, hoy es {date} y revisaremos lo siguiente:\n{clipboard}",
                    Description = "Inserta saludo, fecha de hoy y el portapapeles",
                    Tags = ["reunion", "plantilla"]
                },
                new Snippet
                {
                    Id = "phone",
                    Trigger = "!tel",
                    Title = "Teléfono de Contacto",
                    Content = "+34 612 345 678",
                    Description = "Número de teléfono con prefijo",
                    Tags = ["contacto"]
                },
                new Snippet
                {
                    Id = "uuid",
                    Trigger = "!uuid",
                    Title = "Generador de UUID / GUID",
                    Content = "{uuid}",
                    Description = "Genera un identificador único aleatorio",
                    Tags = ["dev", "uuid"]
                },
                new Snippet
                {
                    Id = "date-today",
                    Trigger = "!fecha",
                    Title = "Fecha de Hoy",
                    Content = "{date}",
                    Description = "Inserta la fecha actual",
                    Tags = ["fecha", "util"]
                }
            ];
        }

        return
        [
            new Snippet
            {
                Id = "email-work",
                Trigger = "!email",
                Title = "Work Email",
                Content = "john.doe@company.com",
                Description = "Professional corporate email",
                Tags = ["work", "email"]
            },
            new Snippet
            {
                Id = "email-personal",
                Trigger = "!email",
                Title = "Personal Email",
                Content = "johndoe@gmail.com",
                Description = "Personal email address",
                Tags = ["personal", "email"]
            },
            new Snippet
            {
                Id = "meeting-intro",
                Trigger = "!intro",
                Title = "Meeting Intro",
                Content = "Hi everyone, today is {date} and we will cover:\n{clipboard}",
                Description = "Greeting with today's date and clipboard content",
                Tags = ["meeting", "template"]
            },
            new Snippet
            {
                Id = "phone",
                Trigger = "!phone",
                Title = "Contact Phone",
                Content = "+1 (555) 123-4567",
                Description = "Phone number with area code",
                Tags = ["contact"]
            },
            new Snippet
            {
                Id = "uuid",
                Trigger = "!uuid",
                Title = "UUID / GUID Generator",
                Content = "{uuid}",
                Description = "Generates a random unique identifier",
                Tags = ["dev", "uuid"]
            },
            new Snippet
            {
                Id = "date-today",
                Trigger = "!date",
                Title = "Today's Date",
                Content = "{date}",
                Description = "Inserts the current date",
                Tags = ["date", "util"]
            }
        ];
    }

    public IReadOnlyList<Snippet> GetAll()
    {
        EnsureUpToDate();
        lock (_lock)
        {
            return _snippets.ToList();
        }
    }

    public IReadOnlyList<Snippet> GetByTrigger(string trigger)
    {
        if (string.IsNullOrWhiteSpace(trigger)) return [];
        EnsureUpToDate();
        lock (_lock)
        {
            return _snippets
                .Where(s => string.Equals(s.Trigger.Trim(), trigger.Trim(), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }

    public IReadOnlyList<TriggerOptionItem> GetResolvedOptionsForTrigger(string trigger)
    {
        var matchingSnippets = GetByTrigger(trigger);
        var result = new List<TriggerOptionItem>();

        foreach (var s in matchingSnippets)
        {
            if (s.Options.Count > 0)
            {
                int index = 1;
                foreach (var opt in s.Options)
                {
                    result.Add(new TriggerOptionItem
                    {
                        Title = s.Options.Count == 1 ? s.Title : $"{s.Title} #{index++}",
                        Content = opt,
                        Trigger = s.Trigger,
                        ParentSnippet = s
                    });
                }
            }
            else if (!string.IsNullOrEmpty(s.Content))
            {
                result.Add(new TriggerOptionItem
                {
                    Title = s.Title,
                    Content = s.Content,
                    Trigger = s.Trigger,
                    ParentSnippet = s
                });
            }
        }

        return result;
    }

    public IReadOnlyList<Snippet> Search(string query)
    {
        EnsureUpToDate();
        if (string.IsNullOrWhiteSpace(query))
        {
            return GetAll();
        }

        query = query.Trim();
        lock (_lock)
        {
            return _snippets
                .Where(s =>
                    s.Trigger.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    s.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    s.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    s.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }
    }

    public void AddOrUpdate(Snippet snippet)
    {
        lock (_lock)
        {
            var idx = _snippets.FindIndex(s => s.Id == snippet.Id);
            if (idx >= 0)
            {
                _snippets[idx] = snippet;
            }
            else
            {
                _snippets.Add(snippet);
            }
            SaveSnippetsInternal();
        }
        OnChanged?.Invoke();
    }

    public bool Delete(string id)
    {
        bool removed;
        lock (_lock)
        {
            removed = _snippets.RemoveAll(s => s.Id == id) > 0;
            if (removed)
            {
                SaveSnippetsInternal();
            }
        }
        if (removed)
        {
            OnChanged?.Invoke();
        }
        return removed;
    }

    private void SaveSnippetsInternal()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_snippets, options);
            File.WriteAllText(_snippetsFilePath, json);
            _lastLoadedUtc = File.Exists(_snippetsFilePath) ? File.GetLastWriteTimeUtc(_snippetsFilePath) : DateTime.UtcNow;
        }
        catch { }
    }

    private void SaveConfig()
    {
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_config, options);
            File.WriteAllText(_configFilePath, json);
        }
        catch { }
    }
}

public sealed class TriggerOptionItem
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Trigger { get; set; } = string.Empty;
    public Snippet ParentSnippet { get; set; } = null!;
}
