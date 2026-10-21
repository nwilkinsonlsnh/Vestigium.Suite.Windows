using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Vestigium.Controls.QueryBar;
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

    private void OnCommit(object sender, RoutedEventArgs e)
    {
        if (DataContext is BloatwareViewModel host)
            host.CommitQuery();
    }

    private void OnPin(object sender, VestigiumQueryRowEventArgs e)
    {
        if (DataContext is BloatwareViewModel host)
            host.Pin(e.Row);
    }

    private void Menu_Opened(object sender, RoutedEventArgs e)
    {
        var installed = DataContext is BloatwareViewModel { Selected.Installed: true };
        AddItem.Visibility = installed ? Visibility.Collapsed : Visibility.Visible;
        RemoveItem.Visibility = installed ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not BloatwareViewModel { Selected: { } row } || row.Installed)
            return;
        _ = Run("Add", row);
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not BloatwareViewModel { Selected: { } row } || !row.Installed)
            return;
        _ = Run("Remove", row);
    }

    private async Task Run(string action, BloatwareRow row)
    {
        if (DataContext is not BloatwareViewModel host)
            return;
        var owner = Window.GetWindow(this);
        var correlationId = Guid.NewGuid().ToString("N");
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Vestigium", "Jobs", "AppMgmtIQ");
        Directory.CreateDirectory(dir);
        var jobPath = Path.Combine(dir, correlationId + ".json");
        var resultPath = Path.ChangeExtension(jobPath, ".result.json");
        var job = new Vestigium.Suite.Windows.AppMgmtIQ.Bloatware.BloatwareJob
        {
            CorrelationId = correlationId,
            Action = action,
            Name = row.Name,
            Family = row.Family,
            Package = row.Package,
            Provisioned = row.Provisioned
        };
        File.WriteAllText(jobPath, System.Text.Json.JsonSerializer.Serialize(job));
        HostLog.Information(AppMgmtCatalog.BloatwareRequested, "Bloatware", $"{action} requested. {row.Family}", correlationId);
        var progress = new BloatwareProgressWindow(action, row.Name) { Owner = owner };
        progress.Loaded += async (_, _) => await Finish(progress, host, action, row, resultPath);
        var exe = Path.Combine(AppContext.BaseDirectory, "AppMgmtIQ.Bloatware.exe");
        if (!File.Exists(exe))
        {
            new ActiveSetupSummaryWindow(action + " Bloatware", "The worker was not found.", row.Name, exe) { Owner = owner }.ShowDialog();
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo(exe, "\"" + jobPath + "\"") { UseShellExecute = true, Verb = "runas" });
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            new ActiveSetupSummaryWindow(action + " Bloatware", "The change was not elevated.", row.Name, ex.Message) { Owner = owner }.ShowDialog();
            return;
        }
        progress.ShowDialog();
    }

    private static async Task Finish(BloatwareProgressWindow progress, BloatwareViewModel host, string action, BloatwareRow row, string resultPath)
    {
        var result = await Wait(resultPath);
        var ok = result?.Ok == true;
        var outcome = ok ? result?.Detail ?? "The change was applied." : result?.Error ?? "The change failed.";
        var detail = string.IsNullOrWhiteSpace(result?.Detail) ? "" : result.Detail;
        progress.Complete(outcome, detail);
        if (ok)
            host.RefreshCommand.Execute(null);
    }

    private static string First(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "";

    private static async Task<Vestigium.Suite.Windows.AppMgmtIQ.Bloatware.BloatwareResult?> Wait(string path)
    {
        var until = DateTime.UtcNow.AddMinutes(30);
        while (DateTime.UtcNow < until)
        {
            if (File.Exists(path))
            {
                try { return System.Text.Json.JsonSerializer.Deserialize<Vestigium.Suite.Windows.AppMgmtIQ.Bloatware.BloatwareResult>(File.ReadAllText(path)); }
                catch (IOException) { }
            }
            await Task.Delay(500);
        }
        return null;
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
