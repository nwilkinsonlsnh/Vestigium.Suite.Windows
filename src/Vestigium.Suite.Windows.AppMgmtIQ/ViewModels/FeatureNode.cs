using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed class FeatureNode : INotifyPropertyChanged
{
    public required string Name { get; init; }
    public required string Caption { get; init; }
    public required string State { get; init; }
    public required bool IsEnabled { get; init; }
    public string Parent { get; init; } = "";
    public FeatureRow? Feature { get; init; }
    public ObservableCollection<FeatureNode> Children { get; } = [];

    private bool _isExpanded;

    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value)
                return;
            _isExpanded = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
