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
    private const uint Directory = 0x10;
    private const string ThisPC = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";

    private static readonly ConcurrentDictionary<string, ImageSource> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static ImageSource? For(string path, bool folder)
    {
        if (path.Length == 0)
            return null;
        var key = folder ? path : Extension(path);
        return Cache.GetOrAdd(key, _ => Load(folder ? path : "file" + key, folder, attributes: !folder));
    }

    public static ImageSource? Computer()
        => Cache.GetOrAdd("thispc", _ => Load(ThisPC, folder: true, attributes: false));

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
