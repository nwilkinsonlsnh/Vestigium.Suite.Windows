using System.Windows;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public partial class DetailsWindow : Window
{
    public DetailsWindow(PackageRow package)
    {
        DataContext = new DetailsViewModel(package);
        InitializeComponent();
    }
}
