using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Vestigium.Suite.Windows.FXplorerIQ.Icons;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class FolderNode : INotifyPropertyChanged
{
    private bool _isExpanded;

    public FolderNode(string name, string fullPath)
    {
        Name = name;
        FullPath = fullPath;
    }

    public string Name { get; }

    public string FullPath { get; }

    public DriveType Drive { get; init; } = DriveType.NoRootDirectory;

    public bool Hidden { get; init; }

    public ObservableCollection<FolderNode> Children { get; } = [];

    public bool Loaded { get; set; }

    public ImageSource? Icon
        => FullPath.Length == 0
            ? null
            : IconArt.For(IconCatalog.For(FullPath, IsExpanded, Hidden, Drive));

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
                return;
            _isExpanded = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Icon));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}