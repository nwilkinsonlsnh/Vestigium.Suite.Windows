using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public partial class DetailsWindow : Window
{
    public DetailsWindow(PackageRow package)
    {
        DataContext = new DetailsViewModel(package);
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateLayout();
        var first = FindRadio(TabStrip);
        if (first is not null)
            first.IsChecked = true;
    }

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DetailsViewModel vm || sender is not RadioButton { DataContext: DetailsTab tab })
            return;
        vm.SelectedTab = tab;
    }

    private static RadioButton? FindRadio(DependencyObject start)
    {
        var count = VisualTreeHelper.GetChildrenCount(start);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(start, i);
            if (child is RadioButton radio)
                return radio;
            var nested = FindRadio(child);
            if (nested is not null)
                return nested;
        }
        return null;
    }
}
