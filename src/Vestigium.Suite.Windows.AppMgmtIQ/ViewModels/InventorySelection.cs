namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class MainViewModel
{
    public event Action<PackageRow>? PackageSelected;

    public void Select(PackageRow package)
    {
        if (!Packages.Contains(package))
        {
            Status = $"{package.DisplayName} is hidden by the current filter.";
            return;
        }
        SelectedPackage = package;
        PackageSelected?.Invoke(package);
    }
}
