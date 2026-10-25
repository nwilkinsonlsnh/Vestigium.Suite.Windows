using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Vestigium.Suite.Windows.FXplorerIQ;

/// <summary>Shell icons, cached. Files by extension, folders and drives by path, This PC by its CLSID.</summary>
public static class ShellIcons
{
    private const uint Icon = 0x100;
    private const uint Small = 0x1;
    private const uint UseAttributes = 0x10;
    private const uint Attributes = 0x200;
    private const uint PidL = 0x8;
    private const uint Directory = 0x10;
    private const int Drives = 0x11;

    private static readonly Dictionary<string, Guid> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Contacts"] = new("56784854-C6CB-462B-8169-88E350ACB882"),
        ["Favorites"] = new("1777F761-68AD-4D8A-87BD-30B759FA33DD"),
        ["OneDrive"] = new("A52BBA46-E9E1-435F-B3D9-28DAA648C0F6")
    };

    private static readonly ConcurrentDictionary<string, ImageSource> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static ImageSource? For(string path, bool folder)
    {
        if (path.Length == 0)
            return null;
        if (folder && Known.TryGetValue(Path.GetFileName(path.TrimEnd('\\')), out var id))
            return Cache.GetOrAdd(id.ToString(), _ => FromKnown(id));
        var key = folder ? path : Extension(path);
        return Cache.GetOrAdd(key, _ => Load(folder ? path : "file" + key, folder, attributes: !folder));
    }

    public static ImageSource? Computer()
        => Cache.GetOrAdd("thispc", _ => FromSpecial(Drives));

    private static ImageSource FromKnown(Guid id)
    {
        if (SHGetKnownFolderIDList(id, 0, IntPtr.Zero, out var pidl) != 0 || pidl == IntPtr.Zero)
            return new BitmapImage();
        try
        {
            return FromPidl(pidl);
        }
        finally
        {
            Marshal.FreeCoTaskMem(pidl);
        }
    }

    private static ImageSource FromSpecial(int folder)
    {
        if (SHGetSpecialFolderLocation(IntPtr.Zero, folder, out var pidl) != 0 || pidl == IntPtr.Zero)
            return new BitmapImage();
        try
        {
            return FromPidl(pidl);
        }
        finally
        {
            Marshal.FreeCoTaskMem(pidl);
        }
    }

    private static ImageSource FromPidl(IntPtr pidl)
    {
        var info = new ShFileInfo();
        SHGetFileInfo(pidl, 0, ref info, (uint)Marshal.SizeOf<ShFileInfo>(), Icon | Small | PidL);
        return Freeze(info);
    }

    private static string Extension(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Length == 0 ? path : ext;
    }

    private static ImageSource Load(string path, bool folder, bool attributes)
    {
        var info = new ShFileInfo();
        var flags = Icon | Small | (attributes ? UseAttributes : 0);
        var attr = folder ? Directory : 0u;
        SHGetFileInfo(path, attr, ref info, (uint)Marshal.SizeOf<ShFileInfo>(), flags);
        return Freeze(info);
    }

    private static ImageSource Freeze(ShFileInfo info)
    {
        if (info.Icon == IntPtr.Zero)
            return new BitmapImage();
        try
        {
            var source = Imaging.CreateBitmapSourceFromHIcon(info.Icon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        finally
        {
            DestroyIcon(info.Icon);
        }
    }

    [DllImport("shell32.dll")]
    private static extern int SHGetKnownFolderIDList(Guid id, uint flags, IntPtr token, out IntPtr pidl);

    [DllImport("shell32.dll")]
    private static extern int SHGetSpecialFolderLocation(IntPtr window, int folder, out IntPtr pidl);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(IntPtr pidl, uint attributes, ref ShFileInfo info, uint size, uint flags);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(string path, uint attributes, ref ShFileInfo info, uint size, uint flags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr handle);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct ShFileInfo
    {
        public IntPtr Icon;
        public int Index;
        public uint Attributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string Display;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string Type;
    }
}
