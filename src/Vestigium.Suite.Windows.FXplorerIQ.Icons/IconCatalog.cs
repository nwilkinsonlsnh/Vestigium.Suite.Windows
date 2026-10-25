using System.IO;

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
        if (Profile(path) is { } special)
            return special;
        if (SystemFolders.Any(folder => Is(path, folder)))
            return expanded ? IconKind.SystemFolderOpen : IconKind.SystemFolder;
        if (hidden)
            return expanded ? IconKind.HiddenFolderOpen : IconKind.HiddenFolder;
        return expanded ? IconKind.FolderOpen : IconKind.Folder;
    }

    private static IconKind? Profile(string path)
    {
        var parts = path.TrimEnd('\\').Split('\\');
        if (parts.Length != 4 || !parts[1].Equals("Users", StringComparison.OrdinalIgnoreCase))
            return null;
        return parts[3].ToLowerInvariant() switch
        {
            "desktop" => IconKind.Desktop,
            "documents" => IconKind.Documents,
            "downloads" => IconKind.Downloads,
            "favorites" => IconKind.Favorites,
            "pictures" => IconKind.Pictures,
            "contacts" => IconKind.Contacts,
            "music" => IconKind.Music,
            "videos" => IconKind.Videos,
            _ => null
        };
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
