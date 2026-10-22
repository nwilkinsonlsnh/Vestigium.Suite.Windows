using System.Collections.ObjectModel;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class FolderNode
{
    public FolderNode(string name, string fullPath)
    {
        Name = name;
        FullPath = fullPath;
    }

    public string Name { get; }

    public string FullPath { get; }

    public ObservableCollection<FolderNode> Children { get; } = [];

    public bool Loaded { get; set; }
}
