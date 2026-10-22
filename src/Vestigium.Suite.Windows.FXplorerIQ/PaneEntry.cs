using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public sealed class PaneEntry : INotifyPropertyChanged
{
    private string _hash;

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
        _hash = hash;
        Category = category;
        Tag = tag;
    }

    public string Name { get; }

    public string FullPath { get; }

    public bool IsDirectory { get; }

    public string DateModified { get; }

    public string Type { get; }

    public string SizeText { get; }

    public string Hash
    {
        get => _hash;
        set
        {
            if (string.Equals(_hash, value, StringComparison.Ordinal))
                return;
            _hash = value;
            OnPropertyChanged();
        }
    }

    public string Category { get; }

    public string Tag { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
