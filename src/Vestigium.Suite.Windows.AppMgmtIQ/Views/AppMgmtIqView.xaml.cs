using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

public partial class AppMgmtIqView : UserControl
{
    private DetailsWindow? _details;

    public AppMgmtIqView()
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

    private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        ShowOrUpdateDetails();
    }

    private void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_details is { IsVisible: true })
            ShowOrUpdateDetails();
    }

    private void ShowOrUpdateDetails()
    {
        if (DataContext is not MainViewModel vm || vm.SelectedPackage is null)
            return;

        if (_details is null || !_details.IsLoaded)
        {
            _details = new DetailsWindow
            {
                Owner = Window.GetWindow(this)
            };
            _details.Closed += (_, _) => _details = null;
            _details.Show();
        }

        _details.SetPackage(vm.SelectedPackage);
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
