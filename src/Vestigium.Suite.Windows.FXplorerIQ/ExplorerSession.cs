using System.IO;
using System.Text.Json;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class ExplorerRecord
{
    public string? Name { get; set; }

    public string Path { get; set; } = "";

    public bool Split { get; set; }

    public string SecondPath { get; set; } = "";

    public bool SourceIsFirst { get; set; } = true;
}

public sealed class ExplorerSession
{
    public List<ExplorerRecord> Explorers { get; set; } = [];
}

public sealed class ExplorerSessionStore
{
    private static readonly JsonSerializerOptions Json =
        new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public ExplorerSessionStore(string rootDirectory)
    {
        RootDirectory = rootDirectory;
        FilePath = Path.Combine(rootDirectory, "session.json");
    }

    public string RootDirectory { get; }

    public string FilePath { get; }

    public ExplorerSession Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new ExplorerSession();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<ExplorerSession>(json, Json) ?? new ExplorerSession();
        }
        catch (Exception)
        {
            return new ExplorerSession();
        }
    }

    public void Save(ExplorerSession session)
    {
        Directory.CreateDirectory(RootDirectory);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(session, Json));
    }
}
