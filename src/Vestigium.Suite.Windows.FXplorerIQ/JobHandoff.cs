using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Vestigium.Suite.Windows.FXplorerIQ;

public static class JobHandoff
{
    public static bool Offer(JobVerb verb, string source, string? destination, bool secure, bool audit)
    {
        var answer = MessageBox.Show(
            "This folder needs elevation. Run it once as administrator?",
            "Elevate",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);
        if (answer != MessageBoxResult.OK)
            return false;

        var settings = App.Settings.Load();
        Directory.CreateDirectory(JobVerify.Root);
        var path = Path.Combine(JobVerify.Root, "fio-" + Guid.NewGuid().ToString("N") + ".json");
        var job = new
        {
            Verb = verb.ToString(),
            Source = source,
            Destination = destination,
            Collision = settings.Collision,
            Pattern = settings.UniqueNamePattern,
            Purge = verb == JobVerb.Mirror,
            Audit = audit,
            Secure = secure
        };
        File.WriteAllText(path, JsonSerializer.Serialize(job));

        var exe = Path.Combine(AppContext.BaseDirectory, "FXplorerIQ.FileOps.exe");
        if (!File.Exists(exe))
        {
            MessageBox.Show("FXplorerIQ.FileOps.exe was not found next to the host.", "Elevate", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        try
        {
            Process.Start(new ProcessStartInfo(exe, "\"" + path + "\"") { UseShellExecute = true, Verb = "runas" });
            return true;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
