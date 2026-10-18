using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

public partial class FeaturesView : UserControl
{
    private SettingsViewModel? _settings;
    public FeaturesView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void Plan_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox box || box.DataContext is not FeatureNode node || DataContext is not FeaturesViewModel host)
            return;
        host.SetBranch(node, node.CheckState != true);
    }

    private async void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not FeaturesViewModel host)
            return;
        var correlationId = Guid.NewGuid().ToString("N");
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Vestigium", "AppMgmtIQ", "Jobs");
        Directory.CreateDirectory(dir);
        var jobPath = Path.Combine(dir, correlationId + ".json");
        var resultPath = Path.ChangeExtension(jobPath, ".result.json");
        var job = new Vestigium.Suite.Windows.AppMgmtIQ.Features.FeatureJob
        {
            CorrelationId = correlationId,
            Enable = host.Enables.Select(change => new Vestigium.Suite.Windows.AppMgmtIQ.Features.FeatureJobItem { Caption = change.Caption, Name = change.Name, Reason = change.Reason, IsAbsent = change.IsAbsent }).ToList(),
            Disable = host.Disables.Select(change => new Vestigium.Suite.Windows.AppMgmtIQ.Features.FeatureJobItem { Caption = change.Caption, Name = change.Name, Reason = change.Reason }).ToList()
        };
        File.WriteAllText(jobPath, System.Text.Json.JsonSerializer.Serialize(job));
        HostLog.Information(AppMgmtCatalog.FeatureApplyRequested, "Features", $"Feature apply requested. enable={job.Enable.Count} disable={job.Disable.Count}", correlationId);

        var exe = Path.Combine(AppContext.BaseDirectory, "AppMgmtIQ.Features.exe");
        try
        {
            Process.Start(new ProcessStartInfo(exe, "\"" + jobPath + "\"") { UseShellExecute = true, Verb = "runas" });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            HostLog.Information(AppMgmtCatalog.FeatureApplyCancelled, "Features", "Feature apply was not elevated.", correlationId);
            return;
        }

        var result = await WaitForResultAsync(resultPath);
        if (result is null || result.Cancelled)
        {
            HostLog.Information(AppMgmtCatalog.FeatureApplyCancelled, "Features", "Feature apply cancelled.", correlationId);
            return;
        }
        HostLog.Information(AppMgmtCatalog.FeatureApplyCompleted, "Features", $"Feature apply completed. enabled={result.Enabled.Count} disabled={result.Disabled.Count} failed={result.Failed.Count}", correlationId);
        host.RefreshCommand.Execute(null);
    }

    private static async Task<Vestigium.Suite.Windows.AppMgmtIQ.Features.FeatureResult?> WaitForResultAsync(string path)
    {
        var until = DateTime.UtcNow.AddMinutes(30);
        while (DateTime.UtcNow < until)
        {
            if (File.Exists(path))
            {
                try { return System.Text.Json.JsonSerializer.Deserialize<Vestigium.Suite.Windows.AppMgmtIQ.Features.FeatureResult>(File.ReadAllText(path)); }
                catch (IOException) { }
            }
            await Task.Delay(500);
        }
        return null;
    }

    public void Attach(SettingsViewModel settings)
    {
        _settings = settings;
        if (Window.GetWindow(this) is { } window)
            window.Closing += (_, _) => RememberExpansion();
        else
            Loaded += (_, _) =>
            {
                if (Window.GetWindow(this) is { } owner)
                    owner.Closing += (_, _) => RememberExpansion();
            };
    }

    private void RememberExpansion()
    {
        if (_settings is null || DataContext is not FeaturesViewModel host)
            return;
        if (host.UniformExpansion is bool expanded)
            _settings.FeaturesExpanded = expanded;
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
    }

    private void Menu_Opened(object sender, RoutedEventArgs e)
    {
        foreach (var item in new[] { EnabledItem, DisabledItem, AbsentItem })
            PaintCheck(item);
        var expanded = _settings?.FeaturesExpanded == true;
        ExpandAllItem.IsChecked = expanded;
        CollapseAllItem.IsChecked = !expanded;

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

    private void ExpandAll_Click(object sender, RoutedEventArgs e)
    {
        SetExpanded(FeatureTree.Items.OfType<FeatureNode>(), true);
        if (_settings is not null)
            _settings.FeaturesExpanded = true;
    }

    private void CollapseAll_Click(object sender, RoutedEventArgs e)
    {
        SetExpanded(FeatureTree.Items.OfType<FeatureNode>(), false);
        if (_settings is not null)
            _settings.FeaturesExpanded = false;
    }

    private static void SetExpanded(IEnumerable<FeatureNode> nodes, bool expanded)
    {
        foreach (var node in nodes)
        {
            node.IsExpanded = expanded;
            SetExpanded(node.Children, expanded);
        }
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
