using System.IO;
using System.Text.Json;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class FXplorerSettings
{
    public string? ThemeId { get; set; }
}

public sealed class FXplorerSettingsStore
{
    private static readonly JsonSerializerOptions Json =
        new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public FXplorerSettingsStore(string rootDirectory)
    {
        RootDirectory = rootDirectory;
        FilePath = Path.Combine(rootDirectory, "settings.json");
    }

    public string RootDirectory { get; }

    public string FilePath { get; }

    public static string DefaultRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Vestigium",
        "Settings",
        "FXplorerIQ");

    public FXplorerSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new FXplorerSettings();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<FXplorerSettings>(json, Json) ?? new FXplorerSettings();
        }
        catch (Exception)
        {
            return new FXplorerSettings();
        }
    }

    public void Save(FXplorerSettings settings)
    {
        Directory.CreateDirectory(RootDirectory);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, Json));
    }
}
