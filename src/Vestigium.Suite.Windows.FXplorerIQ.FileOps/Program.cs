using System.Text.Json;
using Vestigium.Helpers.FileIo;

var jobPath = args.Length > 0 ? args[0] : "";
if (string.IsNullOrWhiteSpace(jobPath) || !File.Exists(jobPath))
    return 2;

var job = JsonSerializer.Deserialize<JobFile>(File.ReadAllText(jobPath));
if (job is null)
    return 3;

var options = new FileIoJobOptions
{
    Collision = Enum.TryParse<FileIoCollision>(job.Collision, out var collision) ? collision : FileIoCollision.UniqueName,
    UniqueNamePattern = string.IsNullOrWhiteSpace(job.Pattern) ? ".##" : job.Pattern,
    Purge = job.Purge,
    AuditMode = job.Audit,
    Shred = job.Secure ? FileIoShredRecipe.ZeroRandomZero : null,
    ReconLeadTime = TimeSpan.FromSeconds(2)
};

FileIoJob work = job.Verb switch
{
    "Move" => FileIoHelper.Move(job.Source, job.Destination ?? "", options),
    "Delete" => FileIoHelper.Delete(job.Source, options),
    "Mirror" => FileIoHelper.Mirror(job.Source, job.Destination ?? "", options),
    _ => FileIoHelper.Copy(job.Source, job.Destination ?? "", options)
};

var result = work.RunAsync().GetAwaiter().GetResult();
var resultPath = Path.ChangeExtension(jobPath, ".result.json");
File.WriteAllText(resultPath, JsonSerializer.Serialize(new
{
    result.JobId,
    result.Status,
    result.Copied,
    result.Skipped,
    result.Failed,
    result.Deleted
}));
return result.Status == "Success" ? 0 : 1;

internal sealed class JobFile
{
    public string Verb { get; set; } = "Copy";
    public string Source { get; set; } = "";
    public string? Destination { get; set; }
    public string Collision { get; set; } = "UniqueName";
    public string Pattern { get; set; } = ".##";
    public bool Purge { get; set; }
    public bool Audit { get; set; }
    public bool Secure { get; set; }
}
