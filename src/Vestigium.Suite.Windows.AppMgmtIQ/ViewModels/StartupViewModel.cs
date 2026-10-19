using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers.WinReg;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public sealed partial class StartupViewModel : ObservableObject
{
    private const string Run = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string RunOnce = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce";
    private const string RunOnceEx = @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnceEx";
    private const string UserRun = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string UserRunOnce = @"Software\Microsoft\Windows\CurrentVersion\RunOnce";

    private readonly List<StartupRow> _all = [];
    private MainViewModel? _inventory;

    public ObservableCollection<StartupRow> Rows { get; } = [];
    public IReadOnlyList<StartupRow> All => _all;

    [ObservableProperty]
    private StartupRow? _selected;

    [ObservableProperty]
    private string _status = "Idle";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _loadingMessage = "Reading startup entries\u2026";

    public void Attach(MainViewModel inventory) => _inventory = inventory;

    public async Task LoadAsync()
    {
        IsLoading = true;
        Status = "Reading startup entries\u2026";
        try
        {
            var found = await Task.Run(ReadAll);
            _all.Clear();
            _all.AddRange(found);
            if (_inventory is not null)
                Link(_inventory.AllPackages);
            else
                Apply();
            HostLog.Information(AppMgmtCatalog.StartupLoaded, "Startup", $"Startup loaded. {_all.Count} entries.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void Link(IReadOnlyList<PackageRow> packages)
    {
        var hints = packages.Select(PackageHint.From).Where(hint => hint.HasSignal).ToList();
        foreach (var row in _all)
            row.Package = Match(row, hints);
        Apply();
    }

    [RelayCommand]
    private Task Refresh() => LoadAsync();

    [RelayCommand]
    private void CopyName()
    {
        if (Selected is not null)
            Clipboard.SetText(Selected.Name);
    }

    [RelayCommand]
    private void CopyCommand()
    {
        if (Selected is not null)
            Clipboard.SetText(Selected.Command);
    }

    private void Apply()
    {
        var keep = Selected;
        Rows.Clear();
        foreach (var row in _all.OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase))
            Rows.Add(row);
        Selected = keep is null ? null : Rows.FirstOrDefault(row => row.Name == keep.Name && row.Command == keep.Command && row.Source == keep.Source);
        Status = $"{Rows.Count} entries";
    }

    private static string Match(StartupRow row, List<PackageHint> hints)
    {
        var path = ExtractPath(row.Command);
        if (path.Length > 0)
        {
            foreach (var hint in hints)
            {
                if (hint.Locations.Any(location => path.StartsWith(location, StringComparison.OrdinalIgnoreCase)))
                    return hint.Name;
            }
        }

        foreach (var hint in hints)
        {
            if (hint.Name.Length < 4)
                continue;
            if (row.Name.Contains(hint.Name, StringComparison.OrdinalIgnoreCase) ||
                row.Command.Contains(hint.Name, StringComparison.OrdinalIgnoreCase))
                return hint.Name;
        }

        return "";
    }

    private static string ExtractPath(string command)
    {
        var text = command.Trim();
        if (text.Length == 0)
            return "";
        if (text[0] == '"')
        {
            var end = text.IndexOf('"', 1);
            text = end > 1 ? text[1..end] : text.Trim('"');
        }
        else
        {
            var space = text.IndexOf(' ');
            if (space > 0)
                text = text[..space];
        }

        text = text.Trim().Trim('"');
        if (text.Length == 0 || !(text.Contains('\\') || text.Contains(':')))
            return "";
        try
        {
            return Path.GetFullPath(text);
        }
        catch
        {
            return text;
        }
    }

    private sealed class PackageHint
    {
        public string Name { get; init; } = "";
        public List<string> Locations { get; } = [];
        public bool HasSignal => Locations.Count > 0 || Name.Length >= 4;

        public static PackageHint From(PackageRow package)
        {
            var hint = new PackageHint { Name = package.DisplayName.Trim() };
            Add(hint, Value(package, "InstallLocation"));
            Add(hint, DirectoryOf(package.UninstallString));
            Add(hint, DirectoryOf(Value(package, "QuietUninstallString")));
            return hint;
        }

        private static string Value(PackageRow package, string name)
            => package.Values.FirstOrDefault(value => string.Equals(value.Name, name, StringComparison.OrdinalIgnoreCase))?.DataText ?? "";

        private static void Add(PackageHint hint, string path)
        {
            var root = Normalize(path);
            if (root.Length < 3 || hint.Locations.Contains(root, StringComparer.OrdinalIgnoreCase))
                return;
            hint.Locations.Add(root);
        }

        private static string DirectoryOf(string command)
        {
            var path = ExtractPath(command);
            if (path.Length == 0)
                return "";
            var dir = Path.GetDirectoryName(path);
            return string.IsNullOrWhiteSpace(dir) ? path : dir;
        }

        private static string Normalize(string path)
        {
            var text = path.Trim().Trim('"');
            if (text.Length == 0)
                return "";
            try
            {
                text = Path.GetFullPath(text);
            }
            catch
            {
            }
            return text.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }

    private static List<StartupRow> ReadAll()
    {
        var client = RegistryHelper.Local;
        var rows = new List<StartupRow>();

        ReadValues(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry64, Run, "Run", "HKLM", "64", rows);
        ReadValues(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry32, Run, "Run", "HKLM", "32", rows);
        ReadValues(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry64, RunOnce, "RunOnce", "HKLM", "64", rows);
        ReadValues(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry32, RunOnce, "RunOnce", "HKLM", "32", rows);
        ReadOnceEx(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry64, RunOnceEx, "HKLM", "64", rows);
        ReadOnceEx(client, RegistryHiveKind.LocalMachine, RegistryViewKind.Registry32, RunOnceEx, "HKLM", "32", rows);
        ReadValues(client, RegistryHiveKind.CurrentUser, RegistryViewKind.Registry64, UserRun, "Run", "HKCU", "64", rows);
        ReadValues(client, RegistryHiveKind.CurrentUser, RegistryViewKind.Registry64, UserRunOnce, "RunOnce", "HKCU", "64", rows);
        ReadFolder(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "User", rows);
        ReadFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), "Common", rows);

        return rows;
    }

    private static void ReadValues(
        RegistryClient client,
        RegistryHiveKind hive,
        RegistryViewKind view,
        string path,
        string source,
        string scope,
        string bit,
        List<StartupRow> rows)
    {
        var key = client.GetKey(hive, path, view, RegistryDetailLevel.Full);
        if (key is null)
            return;
        foreach (var value in key.Values)
            Add(rows, value.Name, value.IsDefault, value.DataText, source, scope, bit);
    }

    private static void ReadOnceEx(
        RegistryClient client,
        RegistryHiveKind hive,
        RegistryViewKind view,
        string path,
        string scope,
        string bit,
        List<StartupRow> rows)
    {
        ReadValues(client, hive, view, path, "RunOnceEx", scope, bit, rows);
        foreach (var child in client.ListSubKeys(hive, path, view, RegistryDetailLevel.Identity))
        {
            if (string.Equals(child.Name, "Depend", StringComparison.OrdinalIgnoreCase))
                continue;
            var snap = client.GetKey(hive, child.Path, view, RegistryDetailLevel.Full);
            if (snap is null)
                continue;
            var title = snap.Values.FirstOrDefault(value => value.IsDefault)?.DataText;
            foreach (var value in snap.Values)
            {
                if (value.IsDefault)
                    continue;
                var name = string.IsNullOrWhiteSpace(title)
                    ? $"{child.Name} / {value.Name}"
                    : $"{title} / {value.Name}";
                Add(rows, name, false, value.DataText, "RunOnceEx", scope, bit);
            }
        }
    }

    private static void ReadFolder(string folder, string scope, List<StartupRow> rows)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return;
        foreach (var file in Directory.EnumerateFiles(folder))
        {
            var name = Path.GetFileName(file);
            if (string.Equals(name, "desktop.ini", StringComparison.OrdinalIgnoreCase))
                continue;
            var display = Path.GetFileNameWithoutExtension(file);
            if (string.IsNullOrWhiteSpace(display))
                display = name;
            var command = string.Equals(Path.GetExtension(file), ".lnk", StringComparison.OrdinalIgnoreCase)
                ? ResolveShortcut(file)
                : file;
            Add(rows, display, false, command, "Startup folder", scope, "");
        }
    }

    private static string ResolveShortcut(string path)
    {
        try
        {
            var type = Type.GetTypeFromProgID("WScript.Shell");
            if (type is null)
                return path;
            dynamic shell = Activator.CreateInstance(type)!;
            dynamic link = shell.CreateShortcut(path);
            string target = link.TargetPath ?? "";
            string args = link.Arguments ?? "";
            if (string.IsNullOrWhiteSpace(target))
                return path;
            return string.IsNullOrWhiteSpace(args) ? target : $"{target} {args}".Trim();
        }
        catch
        {
            return path;
        }
    }

    private static void Add(List<StartupRow> rows, string name, bool isDefault, string? command, string source, string scope, string bit)
    {
        var text = command?.Trim() ?? "";
        if (text.Length == 0)
            return;
        rows.Add(new StartupRow
        {
            Name = isDefault || string.IsNullOrWhiteSpace(name) ? "(Default)" : name,
            Command = text,
            Source = source,
            Scope = scope,
            Bit = bit,
            Package = ""
        });
    }
}
