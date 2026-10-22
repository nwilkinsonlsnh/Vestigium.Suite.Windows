        var token = ++_load;
        var algorithm = App.Settings.Load().Algorithm();
        ShowBusy("Loading " + cleaned);
        try
        {
            var full = Path.GetFullPath(path);
            var rows = await Task.Run(() => Read(full)).ConfigureAwait(true);
            if (token != _load)
                return;

            _algorithm = algorithm.ToString();
            _folder = full;
            PathBox.Text = full;
            Listing.ItemsSource = rows;
            UpButton.IsEnabled = Path.GetDirectoryName(full) is not null;
            Say(rows.Count + " item" + (rows.Count == 1 ? "" : "s") + " · " + HashingHelper.AlgorithmName(algorithm));
            Navigated?.Invoke(full);
            HideBusy();
            _ = FillHashes(rows, algorithm, token);
        }
        catch (Exception ex)
        {
            if (token == _load)
                Say(ex.Message);
            if (token == _load)
                HideBusy();
        }
    }

    private static List<PaneEntry> Read(string full)
    {
        var rows = new List<PaneEntry>();
        foreach (var dir in Directory.EnumerateDirectories(full))
        {
            var info = new DirectoryInfo(dir);
            rows.Add(new PaneEntry(info.Name, info.FullName, true, Write(info.LastWriteTime), "File folder", ""));
        }

        foreach (var file in Directory.EnumerateFiles(full))
        {
            var info = new FileInfo(file);
            rows.Add(new PaneEntry(info.Name, info.FullName, false, Write(info.LastWriteTime), Kind(info.Extension), Size(info.Length)));
        }

        rows.Sort(static (a, b) =>
        {
            var kind = b.IsDirectory.CompareTo(a.IsDirectory);
            return kind != 0 ? kind : string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
        });
        return rows;
    }

    private async Task FillHashes(List<PaneEntry> rows, HashingAlgorithm algorithm, int token)
    {
        var files = rows.Where(row => !row.IsDirectory).ToList();
        foreach (var row in files)
        {
            if (token != _load)
                return;
            var hash = await Task.Run(() => Hash(row.FullPath, algorithm)).ConfigureAwait(true);
            if (token != _load)
                return;
            row.Hash = hash;
        }
    }