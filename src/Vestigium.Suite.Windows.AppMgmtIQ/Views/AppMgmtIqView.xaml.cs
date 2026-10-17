using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
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

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var scroller = FindAncestor<ScrollViewer>(this);
        if (scroller is not null)
            scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

        var view = CollectionViewSource.GetDefaultView(PackageGrid.ItemsSource);
        if (view is not null && view.SortDescriptions.Count == 0)
            view.SortDescriptions.Add(new SortDescription(nameof(PackageRow.DisplayName), ListSortDirection.Ascending));
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
