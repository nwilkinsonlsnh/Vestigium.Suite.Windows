namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class PaneEntry
{
    public PaneEntry(string name, string fullPath, bool isDirectory, string sizeText, string lastWriteText)
    {
        Name = name;
        FullPath = fullPath;
        IsDirectory = isDirectory;
        SizeText = sizeText;
        LastWriteText = lastWriteText;
    }

    public string Name { get; }

    public string FullPath { get; }

    public bool IsDirectory { get; }

    public string SizeText { get; }

    public string LastWriteText { get; }
}
