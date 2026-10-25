using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using Vestigium.Helpers.FileIo;
using Vestigium.Helpers.Hashing;
using Vestigium.Helpers.Pdf;

namespace Vestigium.Suite.Windows.FXplorerIQ;

/// <summary>Writes the evidence log as files finish. A small pool hashes. Not a thread per file, and not a pass after.</summary>
public sealed class JobLiveLog
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly string _source;
    private readonly string _destination;
    private readonly HashingAlgorithm _algorithm;
    private readonly string _jobId;
    private readonly string _verb;
    private readonly ConcurrentDictionary<string, byte> _seen = new();
    private readonly ConcurrentQueue<string> _done = new();
    private readonly List<PdfRow> _rows = new();
    private readonly object _gate = new();
    private readonly object _write = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly Task[] _readers;
    private readonly string _logPath;
    private readonly DateTimeOffset _started = DateTimeOffset.UtcNow;
    private int _matched;
    private int _failed;

    public JobLiveLog(FileIoJob job, string source, string destination, HashingAlgorithm algorithm, string verb)
    {
        _source = source;
        _destination = destination;
        _algorithm = algorithm;
        _verb = verb;
        _jobId = job.JobId;
        Directory.CreateDirectory(JobVerify.Root);
        _logPath = Path.Combine(JobVerify.Root, job.JobId + ".jsonl");
        File.WriteAllText(_logPath, "");
        Write(Line(new
        {
            type = "start",
            correlationId = _jobId,
            verb,
            source,
            destination,
            algorithm = algorithm.ToString(),
            startedUtc = DateTimeOffset.UtcNow
        }));
        job.ProgressChanged += (_, progress) => Note(progress);
        _readers = Enumerable.Range(0, 4).Select(_ => Task.Run(Read)).ToArray();
    }

    public string LogPath => _logPath;

    public IReadOnlyList<PdfRow> Rows
    {
        get { lock (_gate) return _rows.ToArray(); }
    }

    public void Drain()
    {
        _stop.Cancel();
        Task.WaitAll(_readers);
        while (_done.TryDequeue(out var relative))
            Record(relative);
        var matched = _matched;
        var failed = _failed;
        Write(Line(new
        {
            type = "end",
            correlationId = _jobId,
            files = matched + failed,
            matched,
            failed,
            result = failed == 0 ? "Passed" : "Failed",
            elapsed = (DateTimeOffset.UtcNow - _started).ToString(),
            endedUtc = DateTimeOffset.UtcNow
        }));
    }

    private void Read()
    {
        while (!_stop.IsCancellationRequested)
        {
            if (_done.TryDequeue(out var relative))
                Record(relative);
            else
                Thread.Sleep(20);
        }
    }

    private void Note(FileIoProgress progress)
    {
        foreach (var file in progress.Active)
            _seen.TryAdd(file.RelativePath, 1);
        var still = new HashSet<string>(progress.Active.Select(file => file.RelativePath));
        foreach (var path in _seen.Keys)
        {
            if (still.Contains(path) || !_seen.TryRemove(path, out _))
                continue;
            _done.Enqueue(path);
        }
    }

    private void Record(string relative)
    {
        var left = Path.Combine(_source, relative);
        var right = Path.Combine(_destination, relative);
        var compared = FileIoHelper.CompareFiles(left, right, _algorithm);
        var leftInfo = Info(left);
        var rightInfo = Info(right);
        if (compared.Equal)
            Interlocked.Increment(ref _matched);
        else
            Interlocked.Increment(ref _failed);
        lock (_gate)
            _rows.Add(new PdfRow
            {
                Relative = relative,
                Equal = compared.Equal,
                SourceDigest = compared.LeftDigest,
                DestinationDigest = compared.RightDigest
            });
        Write(Line(new
        {
            type = "file",
            sourceDirectory = Path.GetDirectoryName(left) ?? "",
            sourceFileName = Path.GetFileName(left),
            sourceExtension = Path.GetExtension(left),
            sourceSize = leftInfo,
            destinationDirectory = Path.GetDirectoryName(right) ?? "",
            destinationFileName = Path.GetFileName(right),
            destinationExtension = Path.GetExtension(right),
            destinationSize = rightInfo,
            hashMatch = compared.Equal,
            sourceHash = compared.LeftDigest,
            destinationHash = compared.RightDigest
        }));
    }

    private static long Info(string path)
    {
        try { return new FileInfo(path).Length; }
        catch (Exception) { return 0; }
    }

    private static string Line(object value) => JsonSerializer.Serialize(value, Json);

    private void Write(string line)
    {
        lock (_write)
            File.AppendAllText(_logPath, line + Environment.NewLine);
    }
}
