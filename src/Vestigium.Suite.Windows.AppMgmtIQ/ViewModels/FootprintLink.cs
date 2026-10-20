using System.IO;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public static class FootprintLink
{
    public static string Match(FootprintRow row, IReadOnlyList<PackageRow> packages)
    {
        var hints = packages.Select(PackageHint.From).Where(hint => hint.HasSignal).ToList();
        var path = ExtractPath(row.Path);
        if (path.Length > 0)
        {
            foreach (var hint in hints)
            {
                if (hint.Locations.Any(location => path.StartsWith(location, StringComparison.OrdinalIgnoreCase)))
                    return hint.Name;
            }
        }

        if (row.Path.StartsWith('{') && row.Path.EndsWith('}'))
        {
            foreach (var hint in hints)
            {
                if (hint.Key.Contains(row.Path, StringComparison.OrdinalIgnoreCase))
                    return hint.Name;
            }
        }

        foreach (var hint in hints)
        {
            if (hint.Name.Length < 4)
                continue;
            if (row.Name.Contains(hint.Name, StringComparison.OrdinalIgnoreCase) ||
                row.Path.Contains(hint.Name, StringComparison.OrdinalIgnoreCase))
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
        public string Key { get; init; } = "";
        public List<string> Locations { get; } = [];
        public bool HasSignal => Locations.Count > 0 || Name.Length >= 4 || Key.Length > 0;

        public static PackageHint From(PackageRow package)
        {
            var hint = new PackageHint { Name = package.DisplayName.Trim(), Key = package.KeyPath };
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
}
