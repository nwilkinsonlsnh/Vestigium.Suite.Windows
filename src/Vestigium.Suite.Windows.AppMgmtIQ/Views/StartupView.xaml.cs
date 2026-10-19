using System.Windows;
using System.Windows.Controls;
using Vestigium.Controls.QueryBar;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

public partial class StartupView : UserControl
{
    public StartupView()
    {
        InitializeComponent();
    }

    private void OnFilter(object sender, RoutedEventArgs e)
    {
        if (DataContext is StartupViewModel host)
            host.ApplyQuery();
    }

    private void OnCommit(object sender, RoutedEventArgs e)
    {
        if (DataContext is StartupViewModel host)
            host.CommitQuery();
    }

    private void OnPin(object sender, VestigiumQueryRowEventArgs e)
    {
        if (DataContext is StartupViewModel host)
            host.Pin(e.Row);
    }
}
