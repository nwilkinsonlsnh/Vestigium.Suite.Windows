using System.Windows;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

namespace Vestigium.Suite.Windows.AppMgmtIQ;

public partial class FeatureDetailsWindow : Window
{
    public FeatureDetailsWindow()
    {
        InitializeComponent();
    }

    public void SetFeature(FeatureRow feature)
    {
        DataContext = feature;
        Title = feature.Caption;
    }
}
