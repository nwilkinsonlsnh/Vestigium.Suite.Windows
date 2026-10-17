using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

public partial class FeaturesView : UserControl
{
    private FeatureDetailsWindow? _details;

    public FeaturesView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var scroller = FindAncestor<ScrollViewer>(this);
        if (scroller is not null)
            scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
    }

    private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is FeaturesViewModel vm)
            vm.SelectedFeature = (e.NewValue as FeatureNode)?.Feature;
    }

    private void Tree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is FeaturesViewModel { SelectedFeature: { } feature })
            ShowDetails(feature);
    }

    private void OpenDetails_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is FeaturesViewModel { SelectedFeature: { } feature })
            ShowDetails(feature);
    }

    private void Menu_Opened(object sender, RoutedEventArgs e)
    {
        var node = FeatureTree.SelectedItem as FeatureNode;
        var expandable = node is { Children.Count: > 0 };
        ExpandItem.IsEnabled = expandable;
        CollapseItem.IsEnabled = expandable;
    }

    private void Expand_Click(object sender, RoutedEventArgs e)
    {
        var container = ContainerFor(FeatureTree.SelectedItem);
        if (container is null || !container.HasItems)
            return;
        container.IsExpanded = true;
        container.BringIntoView();
    }

    private void Collapse_Click(object sender, RoutedEventArgs e)
    {
        var container = ContainerFor(FeatureTree.SelectedItem);
        if (container is null || !container.HasItems)
            return;
        container.IsExpanded = false;
        var root = container;
        while (ItemsControl.ItemsControlFromItemContainer(root) is TreeViewItem parent)
            root = parent;
        root.IsSelected = true;
        root.BringIntoView();
    }

    private TreeViewItem? ContainerFor(object? item)
    {
        return item is null ? null : FindContainer(FeatureTree, item);
    }

    private static TreeViewItem? FindContainer(ItemsControl parent, object item)
    {
        if (parent.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem direct)
            return direct;
        foreach (var child in parent.Items)
        {
            if (parent.ItemContainerGenerator.ContainerFromItem(child) is not TreeViewItem childContainer)
                continue;
            var found = FindContainer(childContainer, item);
            if (found is not null)
                return found;
        }
        return null;
    }

    private void ShowDetails(FeatureRow feature)
    {
        if (_details is null || !_details.IsLoaded)
        {
            _details = new FeatureDetailsWindow { Owner = Window.GetWindow(this) };
            _details.Closed += (_, _) => _details = null;
            _details.Show();
        }
        _details.SetFeature(feature);
        _details.Activate();
    }

    private static T? FindAncestor<T>(DependencyObject start) where T : DependencyObject
    {
        var current = VisualTreeHelper.GetParent(start);
        while (current is not null)
        {
            if (current is T match)
                return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
