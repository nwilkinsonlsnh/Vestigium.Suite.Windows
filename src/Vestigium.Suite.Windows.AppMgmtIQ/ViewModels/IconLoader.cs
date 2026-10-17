using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

/// <summary>
/// Resolves an ARP icon the way Programs and Features does.
/// DisplayIcon first (including resource index), then ProductIcon, then an exe in InstallLocation, then the uninstall/modify executable.
/// </summary>
public static class IconLoader
{
    public static ImageSource? Load(
        string? displayIcon,
        string? installLocation,
        string? uninstallString,
        string? quietUninstallString,
        string? modifyPath,
        string? displayName,
        string? productCode)
    {
        foreach (var candidate in Candidates(displayIcon, installLocation, uninstallString, quietUninstallString, modifyPath, displayName, productCode))
        {
            var source = FromCandidate(candidate);
            if (source is not null)
                return source;
        }
        return null;
    }

    private static IEnumerable<(string Path, int Index)> Candidates(
        string? displayIcon,
        string? installLocation,
        string? uninstallString,
        string? quietUninstallString,
        string? modifyPath,
        string? displayName,
        string? productCode)
    {
        if (TryParseSpec(displayIcon, out var iconPath, out var iconIndex))
            yield return (iconPath, iconIndex);

        foreach (var productIcon in ProductIcons(productCode))
            yield return (productIcon, 0);

        if (TryParseSpec(displayIcon, out iconPath, out _) && !Path.IsPathRooted(iconPath))
        {
            var rooted = Combine(installLocation, iconPath);
            if (rooted is not null)
                yield return (rooted, iconIndex);
        }

        foreach (var exe in BestExecutables(installLocation, displayName))
            yield return (exe, 0);

        foreach (var command in new[] { uninstallString, quietUninstallString, modifyPath })
        {
            if (TryParseSpec(command, out var commandPath, out var commandIndex) && IsBinary(commandPath))
                yield return (commandPath, commandIndex);
        }
    }

    private static ImageSource? FromCandidate((string Path, int Index) candidate)
    {
        var path = Environment.ExpandEnvironmentVariables(candidate.Path);
        if (!File.Exists(path))
            return null;

        if (candidate.Index == 0 && IsImage(path))
        {
            var frame = TryBitmap(path);
            if (frame is not null)
                return frame;
        }

        var extracted = TryExtract(path, candidate.Index);
        if (extracted is not null)
            return extracted;

        return TryShell(path);
    }

    private static IEnumerable<string> ProductIcons(string? productCode)
    {
        var guid = NormalizeGuid(productCode);
        if (guid is null)
            yield break;

        var packed = guid.Trim('{', '}').Replace("-", "");
        using var products = Registry.ClassesRoot.OpenSubKey(@$"Installer\Products\{packed}");
        var productIcon = products?.GetValue("ProductIcon") as string;
        if (!string.IsNullOrWhiteSpace(productIcon))
            yield return productIcon;

        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Installer", guid);
        if (!Directory.Exists(folder))
            yield break;

        foreach (var file in Directory.EnumerateFiles(folder, "*.ico"))
            yield return file;
    }

    private static IEnumerable<string> BestExecutables(string? installLocation, string? displayName)
    {
        var root = Expand(installLocation);
        if (root is null || !Directory.Exists(root))
            yield break;

        var token = Token(displayName);
        var files = SafeFiles(root)
            .Concat(SafeDirs(root).SelectMany(SafeFiles))
            .Where(IsBinary)
            .Where(path => !IsInstaller(path))
            .OrderByDescending(path => Score(path, token))
            .ThenBy(path => path.Length)
            .Take(4);

        foreach (var file in files)
            yield return file;
    }

    private static int Score(string path, string token)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        var score = 0;
        if (token.Length >= 3 && name.Contains(token, StringComparison.OrdinalIgnoreCase))
            score += 10;
        if (path.Count(c => c == Path.DirectorySeparatorChar) <= 3)
            score += 2;
        return score;
    }

    private static bool TryParseSpec(string? raw, out string path, out int index)
    {
        path = "";
        index = 0;
        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var text = raw.Trim();
        if (text.StartsWith("msiexec", StringComparison.OrdinalIgnoreCase))
            return false;

        if (text.StartsWith('"'))
        {
            var end = text.IndexOf('"', 1);
            if (end > 1)
            {
                path = text[1..end];
                var rest = text[(end + 1)..].Trim().TrimStart(',');
                if (int.TryParse(rest, out var parsed))
                    index = parsed;
                return path.Length > 0;
            }
        }

        var comma = text.LastIndexOf(',');
        if (comma > 1 && int.TryParse(text[(comma + 1)..].Trim(), out var idx))
        {
            path = text[..comma].Trim().Trim('"');
            index = idx;
            return path.Length > 0;
        }

        var space = text.IndexOf(' ');
        path = (space > 0 ? text[..space] : text).Trim().Trim('"');
        return path.Length > 0;
    }

    private static ImageSource? TryBitmap(string path)
    {
        try
        {
            var frame = BitmapFrame.Create(new Uri(path), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            frame.Freeze();
            return frame;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static ImageSource? TryExtract(string path, int index)
    {
        var small = new IntPtr[1];
        try
        {
            if (ExtractIconEx(path, index, null, small, 1) == 0 || small[0] == IntPtr.Zero)
            {
                if (index != 0 && ExtractIconEx(path, 0, null, small, 1) == 0)
                    return null;
            }
            if (small[0] == IntPtr.Zero)
                return null;

            var source = Imaging.CreateBitmapSourceFromHIcon(small[0], System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            if (small[0] != IntPtr.Zero)
                DestroyIcon(small[0]);
        }
    }

    private static ImageSource? TryShell(string path)
    {
        var info = new SHFILEINFO();
        try
        {
            var result = SHGetFileInfo(path, 0, ref info, (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_ICON | SHGFI_SMALLICON);
            if (result == IntPtr.Zero || info.hIcon == IntPtr.Zero)
                return null;

            var source = Imaging.CreateBitmapSourceFromHIcon(info.hIcon, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            source.Freeze();
            return source;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            if (info.hIcon != IntPtr.Zero)
                DestroyIcon(info.hIcon);
        }
    }

    private static string? Combine(string? folder, string file)
    {
        var root = Expand(folder);
        return root is null ? null : Path.Combine(root, file);
    }

    private static string? Expand(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        var expanded = Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
        return expanded.Length == 0 ? null : expanded;
    }

    private static string? NormalizeGuid(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var text = value.Trim();
        return text.StartsWith('{') && text.EndsWith('}') && text.Length >= 38 ? text : null;
    }

    private static string Token(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return "";
        var cut = displayName.IndexOfAny([' ', '(', '-']);
        var token = (cut > 0 ? displayName[..cut] : displayName).Trim();
        return token.Length >= 3 ? token : "";
    }

    private static bool IsBinary(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".exe", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".dll", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".ico", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImage(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".ico", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".bmp", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsInstaller(string path)
    {
        var name = Path.GetFileName(path);
        return name.StartsWith("unins", StringComparison.OrdinalIgnoreCase)
            || name.Contains("uninstall", StringComparison.OrdinalIgnoreCase)
            || name.Contains("setup", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> SafeFiles(string folder)
    {
        try { return Directory.EnumerateFiles(folder, "*.exe"); }
        catch (Exception) { return []; }
    }

    private static IEnumerable<string> SafeDirs(string folder)
    {
        try { return Directory.EnumerateDirectories(folder); }
        catch (Exception) { return []; }
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern uint ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[]? phiconLarge, IntPtr[]? phiconSmall, uint nIcons);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_SMALLICON = 0x1;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }
}
