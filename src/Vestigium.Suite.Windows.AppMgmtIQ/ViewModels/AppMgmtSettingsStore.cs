using System.IO;
using System.Text.Json;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed class AppMgmtSettings
{
    public string? ThemeId { get; set; }
    public bool ShowGlobal { get; set; } = true;
    public bool ShowPerUser { get; set; } = true;
    public bool ShowX64 { get; set; } = true;
    public bool ShowX86 { get; set; } = true;
    public bool ShowSystemComponents { get; set; }
    public bool ShowWindowsInstaller { get; set; } = true;
    public bool ShowOtherInstallers { get; set; } = true;
    public string DateFormat { get; set; } = DisplayFormats.DefaultDateFormat.Label;
    public int DecimalPlaces { get; set; } = 4;
    public bool FeaturesExpanded { get; set; }
    public bool ExportPackages { get; set; } = true;
    public bool ExportFeatures { get; set; } = true;
    public bool ExportBloatware { get; set; } = true;
    public bool ExportActiveSetup { get; set; } = true;
    public bool ExportCurrentView { get; set; } = true;
    public bool ExportDetails { get; set; }
}

public sealed class AppMgmtSettingsStore
{
    private static readonly JsonSerializerOptions Json =
        new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public AppMgmtSettingsStore(string rootDirectory)
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
        "AppMgmtIQ");

    public AppMgmtSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return new AppMgmtSettings();
            var json = File.ReadAllText(FilePath);
            return JsonSerializer.Deserialize<AppMgmtSettings>(json, Json) ?? new AppMgmtSettings();
        }
        catch (Exception)
        {
            return new AppMgmtSettings();
        }
    }

    public void Save(AppMgmtSettings settings)
    {
        Directory.CreateDirectory(RootDirectory);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(settings, Json));
    }
}
