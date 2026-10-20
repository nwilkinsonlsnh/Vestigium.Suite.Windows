using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Vestigium.Controls.QueryBar;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

public partial class AppMgmtIqView : UserControl
{
    private DetailsWindow? _details;
    private SettingsViewModel? _settings;

    public AppMgmtIqView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    public void Attach(SettingsViewModel settings)
    {
        _settings = settings;
        PackageGrid.Tag = settings;
        settings.OpenDetailsRequested += (_, package) => ShowOrUpdateDetails(package);
    }

    private void OnFilter(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel host)
            host.ApplyQuery();
    }

    private void OnCommit(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel host)
            host.CommitQuery();
    }

    private void OnPin(object sender, VestigiumQueryRowEventArgs e)
    {
        if (DataContext is MainViewModel host)
            host.Pin(e.Row);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var scroller = FindAncestor<ScrollViewer>(this);
        if (scroller is not null)
            scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

        var view = CollectionViewSource.GetDefaultView(PackageGrid.ItemsSource);
        if (view is not null && view.SortDescriptions.Count == 0)
            view.SortDescriptions.Add(new SortDescription(nameof(PackageRow.DisplayName), ListSortDirection.Ascending));

        if (DataContext is MainViewModel host)
            host.PackageSelected += package => PackageGrid.ScrollIntoView(package);
    }

    private void Grid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var row = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
        if (row is null)
            return;
        row.IsSelected = true;
        row.Focus();
    }

    private void Menu_Opened(object sender, RoutedEventArgs e)
    {
        foreach (var item in new[] { GlobalItem, PerUserItem, X64Item, X86Item, SystemItem, InstallerItem, OtherItem })
            PaintCheck(item);

        var package = DataContext is MainViewModel host ? host.SelectedPackage : null;
        var hasString = !string.IsNullOrWhiteSpace(package?.UninstallString);
        UninstallItem.IsEnabled = hasString;
        var force = package is not null && UninstallPlan.CanForce(package);
        UninstallQuiet.Visibility = force ? Visibility.Visible : Visibility.Collapsed;
        UninstallBasic.Visibility = force ? Visibility.Visible : Visibility.Collapsed;
        ModifyItem.IsEnabled = package?.HasModify == true;
        RepairItem.IsEnabled = package?.HasRepair == true;
        SetBaselineItem.IsEnabled = package is not null;
        ClearBaselineItem.IsEnabled = package is not null && package.BaselineStatus != "none";
    }

    private void UninstallRegistered_Click(object sender, RoutedEventArgs e) => RunUninstall(UninstallMode.Registered);

    private void UninstallQuiet_Click(object sender, RoutedEventArgs e) => RunUninstall(UninstallMode.QuietNone);

    private void UninstallBasic_Click(object sender, RoutedEventArgs e) => RunUninstall(UninstallMode.QuietBasic);

    private async void RunUninstall(UninstallMode mode)
    {
        if (DataContext is not MainViewModel host || host.SelectedPackage is not { } package)
            return;
        await host.UninstallAsync(package, Window.GetWindow(this), mode);
    }

    private async void Modify_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel host || host.SelectedPackage is not { } package)
            return;
        await host.ModifyAsync(package, Window.GetWindow(this));
    }

    private async void Repair_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel host || host.SelectedPackage is not { } package)
            return;
        await host.RepairAsync(package, Window.GetWindow(this));
    }

    private void SetBaseline_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel { SelectedPackage: { } package } host)
            host.SetBaseline(package);
    }

    private void ClearBaseline_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel { SelectedPackage: { } package } host)
            host.ClearBaseline(package);
    }

    private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel { SelectedPackage: { } package })
            ShowOrUpdateDetails(package);
    }

    private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _settings?.RefreshSelection();
        if (_details is { IsVisible: true } && DataContext is MainViewModel { SelectedPackage: { } package })
            ShowOrUpdateDetails(package);
    }

    private void ShowOrUpdateDetails(PackageRow package)
    {
        if (_details is null || !_details.IsLoaded)
        {
            _details = new DetailsWindow
            {
                Owner = Window.GetWindow(this)
            };
            _details.Closed += (_, _) => _details = null;
            _details.Show();
        }

        _details.SetPackage(package);
        _details.Activate();
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
}
