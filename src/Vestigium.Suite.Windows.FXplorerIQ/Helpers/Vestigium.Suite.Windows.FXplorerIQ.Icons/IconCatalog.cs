namespace Vestigium.Suite.Windows.FXplorerIQ.Icons;

/// <summary>Picks the tree icon for a path. System and Users match the folder itself, not its children.</summary>
public static class IconCatalog
{
    private static readonly string[] SystemFolders =
    [
        @"C:\Windows",
        @"C:\Windows\System32",
        @"C:\Windows\SysWOW64"
    ];

    public static IconKind For(string path, bool expanded, bool hidden = false, DriveType drive = DriveType.NoRootDirectory)
    {
        if (IsDrive(path))
            return Drive(drive);
        if (Is(path, @"C:\Users"))
            return expanded ? IconKind.UsersOpen : IconKind.Users;
        if (SystemFolders.Any(folder => Is(path, folder)))
            return expanded ? IconKind.SystemFolderOpen : IconKind.SystemFolder;
        if (hidden)
            return expanded ? IconKind.HiddenFolderOpen : IconKind.HiddenFolder;
        return expanded ? IconKind.FolderOpen : IconKind.Folder;
    }

    private static bool IsDrive(string path)
        => path.Length is 2 or 3 && path[1] == ':';

    private static IconKind Drive(DriveType drive)
        => drive switch
        {
            DriveType.Network => IconKind.NetworkDrive,
            DriveType.Removable => IconKind.ExternalDrive,
            _ => IconKind.FixedDrive
        };

    private static bool Is(string path, string folder)
        => string.Equals(path.TrimEnd('\\'), folder, StringComparison.OrdinalIgnoreCase);
}
