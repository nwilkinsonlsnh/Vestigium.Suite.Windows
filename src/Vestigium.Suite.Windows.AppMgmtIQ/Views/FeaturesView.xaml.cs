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

    private void OnFilter(object sender, RoutedEventArgs e)
    {
        if (DataContext is FeaturesViewModel host)
            host.ApplyQuery();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var scroller = FindAncestor<ScrollViewer>(this);
        if (scroller is not null)
            scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
    }

    private void Tree_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var item = FindAncestor<TreeViewItem>(e.OriginalSource as DependencyObject);
        if (item is null)
            return;
        item.IsSelected = true;
        item.Focus();
    }

    private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is FeaturesViewModel vm)
            vm.SelectedFeature = (e.NewValue as FeatureNode)?.Feature;
        if (_details is { IsVisible: true } && e.NewValue is FeatureNode node)
        {
            var selected = node;
            Dispatcher.BeginInvoke(() =>
            {
                if (_details is { IsVisible: true })
                    _details.SetFeature(selected);
            });
        }
    }

    private void Tree_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FeatureTree.SelectedItem is FeatureNode node)
            ShowDetails(node);
    }

    private void OpenDetails_Click(object sender, RoutedEventArgs e)
    {
        if (FeatureTree.SelectedItem is FeatureNode node)
            ShowDetails(node);
    }

    private void Menu_Opened(object sender, RoutedEventArgs e)
    {
        foreach (var item in new[] { EnabledItem, DisabledItem, AbsentItem })
            PaintCheck(item);

        var container = ContainerFor(FeatureTree.SelectedItem);
        var expandable = container is { HasItems: true };
        var inside = ParentContainer(container) is { HasItems: true };
        ExpandItem.IsEnabled = expandable;
        CollapseItem.IsEnabled = expandable || inside;
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
        if (container is null)
            return;
        var target = container.HasItems ? container : ParentContainer(container);
        if (target is null)
            return;
        target.IsExpanded = false;
        target.IsSelected = true;
        target.BringIntoView();
    }

    private TreeViewItem? ContainerFor(object? item)
    {
        return item is null ? null : FindContainer(FeatureTree, item);
    }

    private static TreeViewItem? ParentContainer(TreeViewItem? item)
    {
        return item is null ? null : ItemsControl.ItemsControlFromItemContainer(item) as TreeViewItem;
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

    private void ShowDetails(FeatureNode node)
    {
        if (_details is null || !_details.IsLoaded)
        {
            _details = new FeatureDetailsWindow { Owner = Window.GetWindow(this) };
            _details.Closed += (_, _) => _details = null;
            _details.Show();
        }
        var first = !_details.IsVisible;
        _details.SetFeature(node);
        if (first)
            _details.Activate();
    }

    private static T? FindAncestor<T>(DependencyObject start) where T : DependencyObject
    {
        var current = start;
        while (current is not null)
        {
            if (current is T match)
                return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }

    private void FilterCheck_Changed(object sender, RoutedEventArgs e) => PaintCheck(sender as MenuItem);

    private static void PaintCheck(MenuItem? item)
    {
        if (item?.Icon is not TextBlock block)
            return;
        var on = item.IsChecked == true;
        block.Text = on ? "\uE73A" : "\uE739";
        block.Foreground = on
            ? new SolidColorBrush(Color.FromRgb(0x3D, 0xDC, 0x84))
            : new SolidColorBrush(Color.FromRgb(0xF4, 0xF7, 0xFB));
    }
}
