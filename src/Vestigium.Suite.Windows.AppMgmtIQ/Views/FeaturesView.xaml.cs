using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

public partial class FeaturesView : UserControl
{
    private FeatureDetailsWindow? _details;
    private bool _started;

    public FeaturesView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        IsVisibleChanged += OnVisibleChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var scroller = FindAncestor<ScrollViewer>(this);
        if (scroller is not null)
            scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;

        var view = CollectionViewSource.GetDefaultView(FeatureGrid.ItemsSource);
        if (view is not null && view.SortDescriptions.Count == 0)
            view.SortDescriptions.Add(new SortDescription(nameof(FeatureRow.Caption), ListSortDirection.Ascending));
        await EnsureLoaded();
    }

    private async void OnVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
            await EnsureLoaded();
    }

    private async Task EnsureLoaded()
    {
        if (_started || DataContext is not FeaturesViewModel vm)
            return;
        _started = true;
        await vm.LoadAsync();
    }

    private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is FeaturesViewModel { SelectedFeature: { } feature })
            ShowDetails(feature);
    }

    private void OpenDetails_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is FeaturesViewModel { SelectedFeature: { } feature })
            ShowDetails(feature);
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
