using System.IO;
using System.Text.Json;
using Vestigium.Helpers.Hashing;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class FXplorerSettings
{
    public const double DefaultTreeWidth = 260;
    public const double DefaultNameWidth = 280;
    public const double DefaultDateModifiedWidth = 160;
    public const double DefaultTypeWidth = 110;
    public const double DefaultSizeWidth = 72;
    public const double DefaultCategoryWidth = 100;
    public const double DefaultTagWidth = 100;
    public const double DefaultHashWidth = 280;

    public string? ThemeId { get; set; }

    public string HashAlgorithm { get; set; } = Vestigium.Helpers.Hashing.HashingAlgorithm.Sha256.ToString();

    public string Collision { get; set; } = "UniqueName";

    public string UniqueNamePattern { get; set; } = ".##";

    public bool AskBeforeOverwrite { get; set; } = true;

    public string VerifyAlgorithm { get; set; } = Vestigium.Helpers.Hashing.HashingAlgorithm.Sha256.ToString();

    public bool HideEmptyDrives { get; set; }

    public bool ShowAllFolders { get; set; }

    public bool ShowHiddenFiles { get; set; } = true;

    public bool ShowFileExtensions { get; set; } = true;

    public bool ShowSystemFiles { get; set; }

    public bool HideJunctions { get; set; } = true;

    public bool SplitVertical { get; set; } = true;

    public double SplitRatio { get; set; } = 0.5;

    public double TreeWidth { get; set; } = DefaultTreeWidth;

    public double NameWidth { get; set; } = DefaultNameWidth;

    public double DateModifiedWidth { get; set; } = DefaultDateModifiedWidth;

    public double TypeWidth { get; set; } = DefaultTypeWidth;

    public double SizeWidth { get; set; } = DefaultSizeWidth;

    public double CategoryWidth { get; set; } = DefaultCategoryWidth;

    public double TagWidth { get; set; } = DefaultTagWidth;

    public double HashWidth { get; set; } = DefaultHashWidth;

    public string ExcludeFileMasks { get; set; } = "";

    public string ExcludeDirectoryMasks { get; set; } = "";

    public long? MinSizeBytes { get; set; }

    public long? MaxSizeBytes { get; set; }

    public int? MinAgeDays { get; set; }

    public int? MaxAgeDays { get; set; }

    public int? MaxDepth { get; set; }

    public bool IncludeEmptyDirectories { get; set; }

    public bool CopyTimestamps { get; set; } = true;

    public int RetryCount { get; set; } = 3;

    public int RetryWaitSeconds { get; set; } = 2;

    public void ResetColumns()
    {
        TreeWidth = DefaultTreeWidth;
        NameWidth = DefaultNameWidth;
        DateModifiedWidth = DefaultDateModifiedWidth;
        TypeWidth = DefaultTypeWidth;
        SizeWidth = DefaultSizeWidth;
        CategoryWidth = DefaultCategoryWidth;
        TagWidth = DefaultTagWidth;
        HashWidth = DefaultHashWidth;
    }

    public double Width(string header)
        => header switch
        {
            "Folders" => TreeWidth,
            "Name" => NameWidth,
            "Date Modified" => DateModifiedWidth,
            "Type" => TypeWidth,
            "Size" => SizeWidth,
            "Category" => CategoryWidth,
            "Tag" => TagWidth,
            "File Hash" => HashWidth,
            _ => 0
        };

    public void SetWidth(string header, double width)
    {
        switch (header)
        {
            case "Folders": TreeWidth = width; break;
            case "Name": NameWidth = width; break;
            case "Date Modified": DateModifiedWidth = width; break;
            case "Type": TypeWidth = width; break;
            case "Size": SizeWidth = width; break;
            case "Category": CategoryWidth = width; break;
            case "Tag": TagWidth = width; break;
            case "File Hash": HashWidth = width; break;
        }
    }

    public HashingAlgorithm Algorithm()
    {
        if (Enum.TryParse<HashingAlgorithm>(HashAlgorithm, true, out var parsed) && HashingHelper.IsSupported(parsed))
            return parsed;
        return Vestigium.Helpers.Hashing.HashingAlgorithm.Sha256;
    }

    public HashingAlgorithm Verify()
    {
        if (Enum.TryParse<HashingAlgorithm>(VerifyAlgorithm, true, out var parsed) && HashingHelper.IsSupported(parsed))
            return parsed;
        return Vestigium.Helpers.Hashing.HashingAlgorithm.Sha256;
    }
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

    public static event Action? Changed;

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
        Changed?.Invoke();
    }
}
