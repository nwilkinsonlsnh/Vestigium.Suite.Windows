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
            IconKind.ThisPC => "M3,2 H13 V10 H3 Z M6,10 V13 H10 V10 M4,13 H12",
            IconKind.Desktop => "M2,3 H14 V11 H2 Z M7,11 V13 H9 V11 M5,13 H11",
            IconKind.Documents => "M4,2 H10 L13,5 V14 H4 Z M10,2 V5 H13",
            IconKind.Downloads => "M8,2 V9 M5,7 L8,10 L11,7 M3,12 H13 V14 H3 Z",
            IconKind.Favorites => "M8,2 L10,6 H14 L11,9 L12,13 L8,11 L4,13 L5,9 L2,6 H6 Z",
            IconKind.Pictures => "M2,3 H14 V13 H2 Z M4,11 L7,7 L9,9 L12,5",
            IconKind.Contacts => "M5,3 A2,2 0 1 1 5,7 A2,2 0 1 1 5,3 M2,9 H8 V13 H2 Z M10,5 H14 M10,8 H14 M10,11 H14",
            IconKind.Music => "M6,3 V11 A2,2 0 1 1 4,9 V5 L12,3 V9 A2,2 0 1 1 10,7",
            IconKind.Videos => "M2,4 H11 V12 H2 Z M11,6 L14,4 V12 L11,10 Z",
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
            IconKind.ThisPC => Freeze("#5B8DEF"),
            IconKind.Desktop => Freeze("#6AA6E0"),
            IconKind.Downloads => Freeze("#3FA37A"),
            IconKind.Favorites => Freeze("#E2B657"),
            IconKind.Music => Freeze("#7A6BB5"),
            IconKind.Videos => Freeze("#C47B4A"),
            _ => Freeze("#E2B657")
        };

    private static SolidColorBrush Freeze(string hex)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        return brush;
    }
}
