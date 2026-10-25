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
    private readonly StreamWriter _writer;
    private readonly CancellationTokenSource _stop = new();
    private readonly Task[] _readers;
    private readonly string _logPath;
    private readonly DateTimeOffset _started = DateTimeOffset.UtcNow;
    private int _matched;
    private int _failed;

    private readonly ConcurrentQueue<string> _retry = new();
    private readonly int _retries;
    private readonly int _waitMs;
    private readonly int _parallel;
    private int _doneCount;
    private int _expected;
    private int _taken;
    private Action<int, int, string>? _progress;

    public JobLiveLog(FileIoJob job, string source, string destination, HashingAlgorithm algorithm, string verb, int retries, int waitMs, int parallel)
    {
        _source = source;
        _destination = destination;
        _algorithm = algorithm;
        _verb = verb;
        _retries = Math.Clamp(retries, 1, 100);
        _waitMs = Math.Clamp(waitMs, 10, 1000);
        _parallel = Math.Clamp(parallel, 1, 32);
        _jobId = job.JobId;
        Directory.CreateDirectory(JobVerify.Root);
        _logPath = Path.Combine(JobVerify.Root, job.JobId + ".jsonl");
        _writer = new StreamWriter(new FileStream(_logPath, FileMode.Create, FileAccess.Write, FileShare.Read)) { AutoFlush = false };
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
        _readers = Enumerable.Range(0, 16).Select(_ => Task.Run(Read)).ToArray();
    }

    public static string ExportRoot => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        "Vestigium",
        "Exports",
        "FXplorerIQ");

    public string LogPath => _logPath;

    public IReadOnlyList<PdfRow> Rows
    {
        get { lock (_gate) return _rows.ToArray(); }
    }

    public void Watch(int expected, Action<int, int, string> progress)
    {
        _expected = expected;
        _progress = progress;
    }

    private int _closed;

    public void Drain()
    {
        var phase = DateTimeOffset.UtcNow;
        Write(Line(new { type = "phase", phase = "verification", state = "began", utc = phase }));
        Report("Verification phase began");
        foreach (var path in _seen.Keys)
        {
            if (!_seen.TryRemove(path, out _))
                continue;
            _done.Enqueue(path);
            Interlocked.Increment(ref _taken);
        }
        var idle = 0;
        var until = DateTimeOffset.UtcNow.AddSeconds(90);
        while (!_done.IsEmpty && DateTimeOffset.UtcNow < until && idle < 600)
        {
            Thread.Sleep(50);
            idle = _readers.All(reader => reader.IsCompleted) ? idle + 1 : 0;
        }
        _stop.Cancel();
        try { Task.WaitAll(_readers); }
        catch (Exception) { /* a hasher faulted; the trailer still closes the file */ }
        var retrying = Enumerable.Range(0, _parallel).Select(_ => Task.Run(Retry)).ToArray();
        while (!_retry.IsEmpty)
            Thread.Sleep(20);
        Task.WaitAll(retrying);
        while (_retry.TryDequeue(out var relative))
            Finish(relative, _retries, "unread");
        if (Interlocked.Exchange(ref _closed, 1) != 0)
            return;
        var matched = _matched;
        var failed = _failed;
        var ended = DateTimeOffset.UtcNow;
        Write(Line(new
        {
            type = "phase",
            phase = "verification",
            state = "ended",
            elapsed = (ended - phase).ToString(),
            utc = ended
        }));
        Write(Line(new
        {
            type = "end",
            correlationId = _jobId,
            files = matched + failed,
            matched,
            failed,
            result = failed == 0 ? "Passed" : "Failed",
            elapsed = (ended - _started).ToString(),
            verificationElapsed = (ended - phase).ToString(),
            endedUtc = ended
        }));
        lock (_gate)
        {
            _writer.Flush();
            _writer.Dispose();
        }
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
            Interlocked.Increment(ref _taken);
        }
    }

    private void Record(string relative)
    {
        var left = Path.Combine(_source, relative);
        var right = Path.Combine(_destination, relative);
        try
        {
            var compared = FileIoHelper.CompareFiles(left, right, _algorithm);
            WriteRow(relative, left, right, compared.Equal, compared.LeftDigest, compared.RightDigest, 1, null);
        }
        catch (Exception)
        {
            _retry.Enqueue(relative);
            Report("Retrying  " + relative);
        }
    }

    private void Retry()
    {
        while (!_stop.IsCancellationRequested || !_retry.IsEmpty)
        {
            if (!_retry.TryDequeue(out var relative))
            {
                Thread.Sleep(20);
                continue;
            }
            var left = Path.Combine(_source, relative);
            var right = Path.Combine(_destination, relative);
            string? reason = null;
            var opened = false;
            for (var attempt = 2; attempt <= _retries; attempt++)
            {
                Thread.Sleep(_waitMs);
                Report("Retrying  " + relative + "  (" + attempt + " / " + _retries + ")");
                try
                {
                    var compared = FileIoHelper.CompareFiles(left, right, _algorithm);
                    WriteRow(relative, left, right, compared.Equal, compared.LeftDigest, compared.RightDigest, attempt, null);
                    opened = true;
                    break;
                }
                catch (Exception ex)
                {
                    reason = ex.Message;
                }
            }
            if (!opened)
                Finish(relative, _retries, reason);
        }
    }

    private void Finish(string relative, int attempts, string? reason)
    {
        var left = Path.Combine(_source, relative);
        var right = Path.Combine(_destination, relative);
        WriteRow(relative, left, right, false, "", "", attempts, reason);
    }

    private void Report(string text)
    {
        var total = Math.Max(_expected, Math.Max(_taken, _doneCount));
        if (total < 1)
            total = 1;
        _progress?.Invoke(_doneCount, total, text);
    }

    private void WriteRow(string relative, string left, string right, bool equal, string? sourceHash, string? destinationHash, int attempts, string? reason)
    {
        var leftInfo = Info(left);
        var rightInfo = Info(right);
        if (equal)
            Interlocked.Increment(ref _matched);
        else
            Interlocked.Increment(ref _failed);
        lock (_gate)
            _rows.Add(new PdfRow
            {
                Relative = relative,
                Equal = equal,
                SourceDigest = sourceHash,
                DestinationDigest = destinationHash
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
            hashMatch = equal,
            attempts,
            reason,
            sourceHash,
            destinationHash
        }));
        var done = Interlocked.Increment(ref _doneCount);
        Report(Path.GetFileName(left));
    }

    private static long Info(string path)
    {
        try { return new FileInfo(path).Length; }
        catch (Exception) { return 0; }
    }

    private static string Line(object value) => JsonSerializer.Serialize(value, Json);

    private void Write(string line)
    {
        lock (_gate)
        {
            _writer.WriteLine(line);
            if (_rows.Count % 50 == 0)
                _writer.Flush();
        }
    }
}
