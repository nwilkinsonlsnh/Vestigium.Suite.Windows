using System.Windows;
using System.Windows.Controls;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

public partial class ActiveSetupView : UserControl
{
    public ActiveSetupView()
    {
        InitializeComponent();
    }

    private void OnFilter(object sender, RoutedEventArgs e)
    {
        if (DataContext is ActiveSetupViewModel host)
            host.ApplyQuery();
    }
}
