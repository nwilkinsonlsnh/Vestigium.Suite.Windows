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

    public bool IsAbsent => Feature?.InstallState == 3;

    private bool _desiredEnabled;
    private string _reason = "";

    public bool DesiredEnabled
    {
        get => _desiredEnabled;
        set
        {
            if (_desiredEnabled == value)
                return;
            _desiredEnabled = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DesiredEnabled)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChanged)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CheckState)));
        }
    }

    public string Reason
    {
        get => _reason;
        set
        {
            if (_reason == value)
                return;
            _reason = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Reason)));
        }
    }

    public bool IsChanged => DesiredEnabled != IsEnabled;

    public bool? CheckState
    {
        get
        {
            var states = SelfAndDescendants().Select(node => node.DesiredEnabled).ToList();
            if (states.All(on => on))
                return true;
            return states.All(on => !on) ? false : null;
        }
    }

    public void NotifyCheckState()
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CheckState)));

    public IEnumerable<FeatureNode> SelfAndDescendants()
    {
        yield return this;
        foreach (var child in Children)
        {
            foreach (var node in child.SelfAndDescendants())
                yield return node;
        }
    }

    public void ResetDesired()
    {
        DesiredEnabled = IsEnabled;
        Reason = "";
    }

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
