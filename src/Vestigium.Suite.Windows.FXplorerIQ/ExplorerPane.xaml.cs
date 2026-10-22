        node.Loaded = true;
        node.Children.Clear();
        try
        {
            foreach (var dir in Directory.EnumerateDirectories(node.FullPath))
            {
                if (!IsFolder(dir))
                    continue;
                var info = new DirectoryInfo(dir);
                var child = new FolderNode(info.Name, info.FullName);
                if (HasSubfolder(info.FullName))
                    child.Children.Add(new FolderNode("", ""));
                node.Children.Add(child);
            }
        }
        catch (Exception ex)
        {
            Say(ex.Message);
        }
    }

    private static bool IsFolder(string path)
    {
        try
        {
            return Directory.Exists(path) && File.GetAttributes(path).HasFlag(FileAttributes.Directory);
        }
        catch
        {
            return false;
        }
    }