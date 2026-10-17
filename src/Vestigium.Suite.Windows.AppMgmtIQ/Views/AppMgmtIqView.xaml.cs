using System.Windows.Controls;
using System.Windows.Input;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ.Views;

public partial class AppMgmtIqView : UserControl
{
    public AppMgmtIqView()
    {
        InitializeComponent();
    }

    private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MainViewModel vm || vm.SelectedPackage is null)
            return;

        var window = new DetailsWindow(vm.SelectedPackage)
        {
            Owner = Window.GetWindow(this)
        };
        window.Show();
    }
}
