using System.Windows;
using System.Windows.Controls;
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
        var first = FindFirstRadio(TabStrip);
        if (first is not null)
            first.IsChecked = true;
    }

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DetailsViewModel vm || sender is not RadioButton { DataContext: DetailsTab tab })
            return;
        vm.SelectedTab = tab;
    }

    private static RadioButton? FindFirstRadio(ItemsControl strip)
    {
        for (var i = 0; i < strip.Items.Count; i++)
        {
            if (strip.ItemContainerGenerator.ContainerFromIndex(i) is ContentPresenter presenter &&
                presenter.ContentTemplate?.FindName("", presenter) is null)
            {
                // ItemTemplate radios are not named. Walk the visual tree.
            }
        }

        return FindRadio(strip);
    }

    private static RadioButton? FindRadio(DependencyObject start)
    {
        var count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(start);
        for (var i = 0; i < count; i++)
        {
            var child = System.Windows.Media.VisualTreeHelper.GetChild(start, i);
            if (child is RadioButton radio)
                return radio;
            var nested = FindRadio(child);
            if (nested is not null)
                return nested;
        }
        return null;
    }
}
