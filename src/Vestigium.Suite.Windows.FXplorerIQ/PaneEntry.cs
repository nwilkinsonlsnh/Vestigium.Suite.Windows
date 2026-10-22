namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class PaneEntry
{
    public PaneEntry(
        string name,
        string fullPath,
        bool isDirectory,
        string dateModified,
        string type,
        string sizeText,
        string hash = "",
        string category = "",
        string tag = "")
    {
        Name = name;
        FullPath = fullPath;
        IsDirectory = isDirectory;
        DateModified = dateModified;
        Type = type;
        SizeText = sizeText;
        Hash = hash;
        Category = category;
        Tag = tag;
    }

    public string Name { get; }

    public string FullPath { get; }

    public bool IsDirectory { get; }

    public string DateModified { get; }

    public string Type { get; }

    public string SizeText { get; }

    public string Hash { get; }

    public string Category { get; }

    public string Tag { get; }
}
