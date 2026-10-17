using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public partial class DetailsWindow : Window
{
    public DetailsWindow()
    {
        InitializeComponent();
    }

    public void SetPackage(PackageRow package)
    {
        var previous = (DataContext as DetailsViewModel)?.SelectedTab?.Header ?? "General";
        var vm = new DetailsViewModel(package);
        vm.SelectedTab = vm.Tabs.FirstOrDefault(t => t.Header == previous) ?? vm.Tabs[0];
        DataContext = vm;
        UpdateLayout();
        CheckRadio(vm.SelectedTab?.Header);
    }

    private void Tab_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DetailsViewModel vm || sender is not RadioButton { DataContext: DetailsTab tab })
            return;
        vm.SelectedTab = tab;
    }

    private void CheckRadio(string? header)
    {
        var match = FindRadio(TabStrip, header) ?? FindRadio(TabStrip, null);
        if (match is not null)
            match.IsChecked = true;
    }

    private static RadioButton? FindRadio(DependencyObject start, string? header)
    {
        var count = VisualTreeHelper.GetChildrenCount(start);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(start, i);
            if (child is RadioButton radio && (header is null || Equals(radio.Content, header) || Equals(radio.DataContext is DetailsTab tab ? tab.Header : null, header)))
                return radio;
            var nested = FindRadio(child, header);
            if (nested is not null)
                return nested;
        }
        return null;
    }
}
