using System.IO;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

public static class IconLoader
{
    public static ImageSource? Load(string? displayIcon)
    {
        var path = Normalize(displayIcon);
        if (path is null || !File.Exists(path))
            return null;

        try
        {
            var frame = BitmapFrame.Create(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            frame.Freeze();
            return frame;
        }
        catch (Exception)
        {
        }

        try
        {
            using var icon = System.Drawing.Icon.ExtractAssociatedIcon(path);
            if (icon is null)
                return null;
            var source = Imaging.CreateBitmapSourceFromHIcon(
                icon.Handle,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static string? Normalize(string? displayIcon)
    {
        if (string.IsNullOrWhiteSpace(displayIcon))
            return null;

        var text = displayIcon.Trim().Trim('"');
        var comma = text.LastIndexOf(',');
        if (comma > 1 && !text[comma - 1].Equals('\\'))
            text = text[..comma];
        return text;
    }
}
