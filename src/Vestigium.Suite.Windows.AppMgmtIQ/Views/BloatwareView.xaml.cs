using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

public partial class BloatwareView : UserControl
{
    public BloatwareView()
    {
        InitializeComponent();
    }

    private void OnFilter(object sender, RoutedEventArgs e)
    {
        if (DataContext is BloatwareViewModel host)
            host.ApplyQuery();
    }

    private void CopyName_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is BloatwareViewModel { Selected: { } row })
            Clipboard.SetText(row.Name);
    }

    private void CopyFamily_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is BloatwareViewModel { Selected: { } row })
            Clipboard.SetText(row.Family);
    }

    private void CopyPackage_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is BloatwareViewModel { Selected: { } row })
            Clipboard.SetText(row.Package);
    }

    private void Grid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var row = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
        if (row is null)
            return;
        row.IsSelected = true;
        row.Focus();
    }

    private static T? FindAncestor<T>(DependencyObject? node) where T : DependencyObject
    {
        while (node is not null)
        {
            if (node is T hit)
                return hit;
            node = System.Windows.Media.VisualTreeHelper.GetParent(node);
        }
        return null;
    }
}
