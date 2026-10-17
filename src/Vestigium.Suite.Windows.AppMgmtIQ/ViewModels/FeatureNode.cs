using System.Collections.ObjectModel;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed class FeatureNode
{
    public required string Name { get; init; }
    public required string Caption { get; init; }
    public required string State { get; init; }
    public required bool IsEnabled { get; init; }
    public FeatureRow? Feature { get; init; }
    public ObservableCollection<FeatureNode> Children { get; } = [];
}
