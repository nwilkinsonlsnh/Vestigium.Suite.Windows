using System.Collections.Concurrent;
using System.IO;
using Vestigium.Helpers.FileIo;
using Vestigium.Helpers.Hashing;
using Vestigium.Helpers.Pdf;

namespace Vestigium.Suite.Windows.FXplorerIQ;

/// <summary>Writes a log line when a file finishes, through the logging buffer. One reader, not a thread per file.</summary>
public sealed class JobLiveLog
{
    private readonly string _source;
    private readonly string _destination;
    private readonly HashingAlgorithm _algorithm;
    private readonly string _jobId;
    private readonly ConcurrentDictionary<string, byte> _seen = new();
    private readonly ConcurrentQueue<string> _done = new();
    private readonly List<PdfRow> _rows = new();
    private readonly object _rowsGate = new();

    private readonly CancellationTokenSource _stop = new();
    private readonly Task _reader;

    public JobLiveLog(FileIoJob job, string source, string destination, HashingAlgorithm algorithm)
    {
        _source = source;
        _destination = destination;
        _algorithm = algorithm;
        _jobId = job.JobId;
        job.ProgressChanged += (_, progress) => Note(progress);
        _reader = Task.Run(Read);
    }

    public IReadOnlyList<PdfRow> Rows
    {
        get { lock (_rowsGate) return _rows.ToArray(); }
    }

    public void Drain()
    {
        _stop.Cancel();
        _reader.Wait();
        while (_done.TryDequeue(out var relative))
            Record(relative);
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
            if (still.Contains(path))
                continue;
            if (_seen.TryRemove(path, out _))
                _done.Enqueue(path);
        }
    }

    private void Record(string relative)
    {
        var left = Path.Combine(_source, relative);
        var right = Path.Combine(_destination, relative);
        var compared = FileIoHelper.CompareFiles(left, right, _algorithm);
        var row = new PdfRow
        {
            Relative = relative,
            Equal = compared.Equal,
            SourceDigest = compared.LeftDigest,
            DestinationDigest = compared.RightDigest
        };
        lock (_rowsGate)
            _rows.Add(row);
        HostLog.Information(
            FXplorerCatalog.JobCompleted,
            "File",
            "id=" + _jobId + " file=" + relative + " equal=" + compared.Equal + " source=" + compared.LeftDigest + " dest=" + compared.RightDigest);
    }
}
