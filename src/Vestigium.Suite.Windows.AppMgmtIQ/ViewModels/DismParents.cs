using System.Runtime.InteropServices;

namespace Vestigium.Suite.Windows.AppMgmtIQ.ViewModels;

/// <summary>
/// Parent map from DismGetFeatureParent. Requires an elevated process. Returns null when DISM refuses.
/// </summary>
internal static class DismParents
{
    private const string Online = "DISM_{53BFAE52-B167-4E2F-A258-0A37B57FF845}";

    public static Dictionary<string, string>? Read()
    {
        if (DismInitialize(2, null, null) != 0)
            return null;

        uint session = 0;
        try
        {
            if (DismOpenSession(Online, null, null, out session) != 0)
                return null;

            if (DismGetFeatures(session, null, 2, out var features, out var count) != 0 || features == IntPtr.Zero)
                return null;

            var names = new List<string>();
            var size = Marshal.SizeOf<DismFeature>();
            for (var i = 0; i < count; i++)
            {
                var feature = Marshal.PtrToStructure<DismFeature>(features + i * size);
                if (!string.IsNullOrWhiteSpace(feature.FeatureName))
                    names.Add(feature.FeatureName);
            }
            DismDelete(features);

            var parents = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var name in names)
            {
                if (DismGetFeatureParent(session, name, null, 2, out var parentPtr, out var parentCount) != 0 || parentPtr == IntPtr.Zero || parentCount == 0)
                    continue;
                var parent = Marshal.PtrToStructure<DismFeature>(parentPtr);
                if (!string.IsNullOrWhiteSpace(parent.FeatureName))
                    parents[name] = parent.FeatureName;
                DismDelete(parentPtr);
            }
            return parents;
        }
        finally
        {
            if (session != 0)
                DismCloseSession(session);
            DismShutdown();
        }
    }

    [DllImport("dismapi.dll", CharSet = CharSet.Unicode)]
    private static extern int DismInitialize(int logLevel, string? logFilePath, string? scratchDirectory);

    [DllImport("dismapi.dll", CharSet = CharSet.Unicode)]
    private static extern int DismShutdown();

    [DllImport("dismapi.dll", CharSet = CharSet.Unicode)]
    private static extern int DismOpenSession(string imagePath, string? windowsDirectory, string? systemDrive, out uint session);

    [DllImport("dismapi.dll", CharSet = CharSet.Unicode)]
    private static extern int DismCloseSession(uint session);

    [DllImport("dismapi.dll", CharSet = CharSet.Unicode)]
    private static extern int DismGetFeatures(uint session, string? identifier, int packageIdentifier, out IntPtr features, out uint count);

    [DllImport("dismapi.dll", CharSet = CharSet.Unicode)]
    private static extern int DismGetFeatureParent(uint session, string featureName, string? identifier, int packageIdentifier, out IntPtr features, out uint count);

    [DllImport("dismapi.dll")]
    private static extern int DismDelete(IntPtr dismStructure);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct DismFeature
    {
        public string FeatureName;
        public int State;
    }
}
