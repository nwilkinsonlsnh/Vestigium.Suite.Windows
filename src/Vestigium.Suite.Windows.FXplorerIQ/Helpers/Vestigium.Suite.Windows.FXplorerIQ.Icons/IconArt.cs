using System.Windows.Media;

namespace Vestigium.Suite.Windows.FXplorerIQ.Icons;

/// <summary>Drawn icons. One geometry per kind, so there is no loose image to lose.</summary>
public static class IconArt
{
    public static DrawingImage For(IconKind kind)
    {
        var image = new DrawingImage(new GeometryDrawing(Brush(kind), null, Geometry.Parse(Path(kind))));
        image.Freeze();
        return image;
    }

    private static string Path(IconKind kind)
        => kind switch
        {
            IconKind.FixedDrive => "M2,6 H14 V16 H2 Z M4,8 H12 V10 H4 Z",
            IconKind.ExternalDrive => "M2,6 H14 V16 H2 Z M6,13 H10 V14.5 H6 Z",
            IconKind.NetworkDrive => "M2,7 H14 V15 H2 Z M5,4 H11 V7 H5 Z M7,15 V17 H9 V15",
            IconKind.FolderOpen => "M1,5 H7 L8,7 H15 V15 H1 Z",
            IconKind.HiddenFolder => "M1,4 H7 L8,6 H15 V14 H1 Z",
            IconKind.HiddenFolderOpen => "M1,5 H7 L8,7 H15 V15 H1 Z",
            IconKind.SystemFolder => "M1,4 H7 L8,6 H15 V14 H1 Z M6,8 H10 V12 H6 Z",
            IconKind.SystemFolderOpen => "M1,5 H7 L8,7 H15 V15 H1 Z M6,9 H10 V13 H6 Z",
            IconKind.Users => "M8,3 A2,2 0 1 1 8,7 A2,2 0 1 1 8,3 M4,9 H12 V14 H4 Z",
            IconKind.UsersOpen => "M8,4 A2,2 0 1 1 8,8 A2,2 0 1 1 8,4 M3,10 H13 V15 H3 Z",
            _ => "M1,4 H7 L8,6 H15 V14 H1 Z"
        };

    private static Brush Brush(IconKind kind)
        => kind switch
        {
            IconKind.FixedDrive => Freeze("#5B8DEF"),
            IconKind.ExternalDrive => Freeze("#3FA37A"),
            IconKind.NetworkDrive => Freeze("#7A6BB5"),
            IconKind.HiddenFolder or IconKind.HiddenFolderOpen => Freeze("#9AA4AD"),
            IconKind.SystemFolder or IconKind.SystemFolderOpen => Freeze("#C47B4A"),
            IconKind.Users or IconKind.UsersOpen => Freeze("#4C8DCA"),
            _ => Freeze("#E2B657")
        };

    private static SolidColorBrush Freeze(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
