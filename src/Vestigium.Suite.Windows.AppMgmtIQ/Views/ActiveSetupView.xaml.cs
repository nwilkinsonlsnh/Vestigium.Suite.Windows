using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Win32;
using Vestigium.Controls.QueryBar;
using Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;
using Worker = Vestigium.Suite.Windows.AppMgmtIQ.ActiveSetup;

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

    private void OnPin(object sender, VestigiumQueryRowEventArgs e)
    {
        if (DataContext is ActiveSetupViewModel host)
            host.Pin(e.Row);
    }

    private async void Add_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ActiveSetupViewModel host)
            return;
        var owner = Window.GetWindow(this);
        var wizard = new ActiveSetupWizardWindow { Owner = owner };
        if (wizard.ShowDialog() != true)
            return;
        var correlationId = Guid.NewGuid().ToString("N");
        if (!wizard.IsGlobal)
        {
            WriteUser(wizard);
            HostLog.Information(AppMgmtCatalog.ActiveSetupChanged, "ActiveSetup", $"Added user entry {wizard.Component}.", correlationId);
            host.RefreshCommand.Execute(null);
            return;
        }
        await RunWorker(host, correlationId, "Add", wizard.DisplayName, wizard.Component, wizard.StubPath, wizard.Version, wizard.IsX64, true);
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ActiveSetupViewModel host || host.Selected is not { } row)
            return;
        var confirm = new ActiveSetupConfirmWindow(row.DisplayName, KeyPath(row)) { Owner = Window.GetWindow(this) };
        if (confirm.ShowDialog() != true)
            return;
        var correlationId = Guid.NewGuid().ToString("N");
        if (!row.IsGlobal)
        {
            DeleteUser(row);
            HostLog.Information(AppMgmtCatalog.ActiveSetupChanged, "ActiveSetup", $"Deleted user entry {row.Component}.", correlationId);
            host.RefreshCommand.Execute(null);
            return;
        }
        await RunWorker(host, correlationId, "Delete", row.DisplayName, row.Component, row.StubPath, row.Version, row.IsX64, row.Installed);
    }

    private async void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ActiveSetupViewModel host || host.Selected is not { } row)
            return;
        var correlationId = Guid.NewGuid().ToString("N");
        if (!row.IsGlobal)
        {
            ClearUser(row);
            HostLog.Information(AppMgmtCatalog.ActiveSetupChanged, "ActiveSetup", $"Cleared user command {row.Component}.", correlationId);
            host.RefreshCommand.Execute(null);
            return;
        }
        await RunWorker(host, correlationId, "ClearStubPath", row.DisplayName, row.Component, row.StubPath, row.Version, row.IsX64, row.Installed);
    }

    private void CopyComponent_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ActiveSetupViewModel { Selected: { } row })
            Clipboard.SetText(row.Component);
    }

    private void CopyStub_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ActiveSetupViewModel { Selected: { } row })
            Clipboard.SetText(row.StubPath);
    }

    private void CopyPath_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is ActiveSetupViewModel { Selected: { } row })
            Clipboard.SetText(KeyPath(row));
    }

    private void Grid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var row = FindAncestor<DataGridRow>(e.OriginalSource as DependencyObject);
        if (row is null)
            return;
        row.IsSelected = true;
        row.Focus();
    }

    private async Task RunWorker(ActiveSetupViewModel host, string correlationId, string action, string name, string component, string stub, string version, bool x64, bool installed)
    {
        var owner = Window.GetWindow(this);
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Vestigium", "AppMgmtIQ", "Jobs");
        Directory.CreateDirectory(dir);
        var jobPath = Path.Combine(dir, correlationId + ".json");
        var resultPath = Path.ChangeExtension(jobPath, ".result.json");
        var job = new Worker.ActiveSetupJob
        {
            CorrelationId = correlationId,
            ThemeId = App.Themes.Current?.Id ?? "",
            ParentLeft = owner?.Left ?? 0,
            ParentTop = owner?.Top ?? 0,
            ParentWidth = owner?.ActualWidth ?? 0,
            ParentHeight = owner?.ActualHeight ?? 0,
            Action = action,
            DisplayName = name,
            Component = component,
            StubPath = stub,
            Version = version,
            IsX64 = x64,
            Installed = installed
        };
        File.WriteAllText(jobPath, System.Text.Json.JsonSerializer.Serialize(job));
        HostLog.Information(AppMgmtCatalog.ActiveSetupRequested, "ActiveSetup", $"{action} requested. {component}", correlationId);
        var exe = Path.Combine(AppContext.BaseDirectory, "AppMgmtIQ.ActiveSetup.exe");
        if (!File.Exists(exe))
        {
            ShowSummary(owner, action, component, "The worker was not found.", exe);
            HostLog.Information(AppMgmtCatalog.ActiveSetupCancelled, "ActiveSetup", $"{action} worker missing. {exe}", correlationId);
            return;
        }
        try
        {
            Process.Start(new ProcessStartInfo(exe, "\"" + jobPath + "\"") { UseShellExecute = true, Verb = "runas" });
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            ShowSummary(owner, action, component, "The change was not elevated.", ex.ToString());
            HostLog.Information(AppMgmtCatalog.ActiveSetupCancelled, "ActiveSetup", $"{action} was not elevated. {ex.Message}", correlationId);
            return;
        }
        var result = await WaitForResultAsync(resultPath);
        if (result is null)
        {
            ShowSummary(owner, action, component, "The worker did not report back.", "No result file was written.");
            HostLog.Information(AppMgmtCatalog.ActiveSetupCancelled, "ActiveSetup", $"{action} did not report back.", correlationId);
            return;
        }
        if (result.Cancelled)
        {
            ShowSummary(owner, action, component, "The change was cancelled.", result.KeyPath);
            HostLog.Information(AppMgmtCatalog.ActiveSetupCancelled, "ActiveSetup", $"{action} cancelled.", correlationId);
            return;
        }
        ShowSummary(owner, action, component, result.Ok ? "The change was applied." : "The change failed.", result.Ok ? result.KeyPath : result.Error);
        HostLog.Information(result.Ok ? AppMgmtCatalog.ActiveSetupChanged : AppMgmtCatalog.ActiveSetupCompleted, "ActiveSetup", result.Ok ? $"{action} applied. {result.KeyPath}" : result.Error, correlationId);
        if (result.Ok)
            host.RefreshCommand.Execute(null);
    }

    private static void ShowSummary(Window? owner, string action, string component, string outcome, string detail)
    {
        var window = new ActiveSetupSummaryWindow(TitleFor(action), outcome, component, detail) { Owner = owner };
        window.ShowDialog();
    }

    private static string TitleFor(string action) => action.ToLowerInvariant() switch
    {
        "delete" => "Delete Active Setup Entry",
        "clearstubpath" => "Clear Active Setup Command",
        _ => "Add Active Setup Entry"
    };

    private static async Task<Worker.ActiveSetupResult?> WaitForResultAsync(string path)
    {
        var until = DateTime.UtcNow.AddMinutes(30);
        while (DateTime.UtcNow < until)
        {
            if (File.Exists(path))
            {
                try { return System.Text.Json.JsonSerializer.Deserialize<Worker.ActiveSetupResult>(File.ReadAllText(path)); }
                catch (IOException) { }
            }
            await Task.Delay(500);
        }
        return null;
    }

    private static void WriteUser(ActiveSetupWizardWindow wizard)
    {
        var view = wizard.IsX64 ? RegistryView.Registry64 : RegistryView.Registry32;
        var path = $@"{(wizard.IsX64 ? "SOFTWARE" : @"SOFTWARE\WOW6432Node")}\Microsoft\Active Setup\Installed Components\{wizard.Component}";
        using var root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view);
        using var key = root.CreateSubKey(path, writable: true);
        key?.SetValue("", wizard.DisplayName, RegistryValueKind.String);
        key?.SetValue("StubPath", wizard.StubPath, RegistryValueKind.String);
        key?.SetValue("Version", wizard.Version, RegistryValueKind.String);
        key?.SetValue("IsInstalled", 1, RegistryValueKind.DWord);
    }

    private static void DeleteUser(ActiveSetupRow row)
    {
        var view = row.IsX64 ? RegistryView.Registry64 : RegistryView.Registry32;
        using var root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view);
        root.DeleteSubKeyTree(Relative(row), throwOnMissingSubKey: false);
    }

    private static void ClearUser(ActiveSetupRow row)
    {
        var view = row.IsX64 ? RegistryView.Registry64 : RegistryView.Registry32;
        using var root = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view);
        using var key = root.OpenSubKey(Relative(row), writable: true);
        key?.DeleteValue("StubPath", throwOnMissingValue: false);
    }

    private static string Relative(ActiveSetupRow row)
        => $@"{(row.IsX64 ? "SOFTWARE" : @"SOFTWARE\WOW6432Node")}\Microsoft\Active Setup\Installed Components\{row.Component}";

    private static string KeyPath(ActiveSetupRow row)
        => $@"{(row.IsGlobal ? "HKLM" : "HKCU")}\{(row.IsX64 ? "SOFTWARE" : @"SOFTWARE\WOW6432Node")}\Microsoft\Active Setup\Installed Components\{row.Component}";

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
