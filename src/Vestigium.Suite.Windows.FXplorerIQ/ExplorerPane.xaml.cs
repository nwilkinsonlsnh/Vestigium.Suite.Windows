    public string Folder => _folder;

    public string SelectionOrFolder()
        => Listing.SelectedItem is PaneEntry entry && entry.FullPath.Length > 0 ? entry.FullPath : _folder;