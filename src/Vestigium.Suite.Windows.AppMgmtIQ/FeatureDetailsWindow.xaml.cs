using System.Windows;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public partial class FeatureDetailsWindow : Window
{
    public FeatureDetailsWindow()
    {
        InitializeComponent();
    }

    public void SetFeature(FeatureNode node)
    {
        var feature = node.Feature;
        DataContext = new FeatureDetail
        {
            Caption = node.Caption,
            Name = node.Name,
            Parent = string.IsNullOrWhiteSpace(node.Parent) ? "(none)" : node.Parent,
            State = node.State,
            Status = feature?.Status ?? "",
            Description = string.IsNullOrWhiteSpace(feature?.Description)
                ? "No description is published for this feature."
                : feature!.Description
        };
        Title = node.Caption;
    }

    private sealed class FeatureDetail
    {
        public required string Caption { get; init; }
        public required string Name { get; init; }
        public required string Parent { get; init; }
        public required string State { get; init; }
        public required string Status { get; init; }
        public required string Description { get; init; }
    }
}
