    private static List<PaneEntry> Read(string full)
    {
        var rows = new List<PaneEntry>();
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(full))
            {
                if (!IsFolder(dir))
                    continue;
                var info = new DirectoryInfo(dir);
                rows.Add(new PaneEntry(info.Name, info.FullName, true, Write(info.LastWriteTime), "File folder", ""));
            }

            foreach (var file in Directory.EnumerateFiles(full))
            {
                var info = new FileInfo(file);
                rows.Add(new PaneEntry(info.Name, info.FullName, false, Write(info.LastWriteTime), Kind(info.Extension), Size(info.Length)));
            }
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }

        rows.Sort(static (a, b) =>
        {
            var kind = b.IsDirectory.CompareTo(a.IsDirectory);
            return kind != 0 ? kind : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });
        return rows;
    }