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
