using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Threading;

namespace Synthesis.Core.Log;

public static class Logger
{
    private const string LogPath = "latest.log";
    private const int MaxUiHistoryEntries = 200;
    private const int MaxQueuedEntries = 8192;
    private const int QueueEnqueueTimeoutMs = 200;
    private const int MaxArchivedLogs = 5;
    private const long MaxLogFileBytes = 5L * 1024L * 1024L;
    private const int MaxFallbackBufferEntries = 512;

    private static readonly Lock _fileLock;
    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private static readonly int NewLineByteCount = Utf8NoBom.GetByteCount(Environment.NewLine);
    private static readonly Channel<LogEntry> _channel;
    private static readonly ConcurrentQueue<string> _fallbackBuffer = new();
    private static readonly CancellationTokenSource _shutdownCts = new();
    private static readonly Task _consumerTask;
    private static long _queuedCount;
    private static long _processedCount;

    static Logger()
    {
        _fileLock = new Lock();
        LogHistory = [];
        _channel = Channel.CreateBounded<LogEntry>(new BoundedChannelOptions(MaxQueuedEntries)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait
        });
        _consumerTask = Task.Run(ProcessQueueAsync);

        var sessionLine = "=== Session Started: " +
                          DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture) +
                          " ===";
        if (TryAppendLines([sessionLine], false))
        {
            AddUiHistory(sessionLine);
        }
    }

    public static ObservableCollection<string> LogHistory { get; }

    public static void Log(string message, LogLevel level = LogLevel.Info) => Log(message, level, null);

    public static void Log(string message, LogLevel level, string? category = null)
    {
        var entry = new LogEntry(
            DateTimeOffset.Now,
            Environment.CurrentManagedThreadId,
            level,
            message ?? string.Empty,
            category);

        Enqueue(entry);
        AddUiHistory(entry.ToLine());
    }

    public static void Info(string msg) => Log(msg);

    public static void Warn(string msg) => Log(msg, LogLevel.Warning);

    public static void Error(string msg, Exception? ex = null)
    {
        if (ex != null)
        {
            msg += Environment.NewLine + ">>> Exception Details: " + ex;
        }

        Log(msg, LogLevel.Error);
    }

    public static async Task FlushAsync()
    {
        var spinCount = 0;
        while (Interlocked.Read(ref _processedCount) < Interlocked.Read(ref _queuedCount) && spinCount < 200)
        {
            spinCount++;
            await Task.Delay(20).ConfigureAwait(false);
        }

        for (var retry = 0; retry < 3 && !_fallbackBuffer.IsEmpty; retry++)
        {
            FlushFallbackBuffer();
            if (!_fallbackBuffer.IsEmpty)
            {
                await Task.Delay(20).ConfigureAwait(false);
            }
        }
    }

    private static void Enqueue(in LogEntry entry)
    {
        Interlocked.Increment(ref _queuedCount);
        if (_channel.Writer.TryWrite(entry))
        {
            return;
        }

        try
        {
            using var timeoutCts = new CancellationTokenSource(QueueEnqueueTimeoutMs);
            _channel.Writer.WriteAsync(entry, timeoutCts.Token).AsTask().GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            var congestionLine =
                CreateSystemLine(LogLevel.Warning, "Logger", "Queue congested, switched to sync write.");
            TryAppendLines([congestionLine, entry.ToLine()], true);
            Interlocked.Increment(ref _processedCount);
        }
        catch
        {
            var failureLine =
                CreateSystemLine(LogLevel.Error, "Logger", "Queue enqueue failed, switched to sync write.");
            TryAppendLines([failureLine, entry.ToLine()], true);
            Interlocked.Increment(ref _processedCount);
        }
    }

    private static async Task ProcessQueueAsync()
    {
        var reader = _channel.Reader;
        var batch = new List<LogEntry>(128);

        try
        {
            while (await reader.WaitToReadAsync(_shutdownCts.Token).ConfigureAwait(false))
            {
                batch.Clear();
                while (batch.Count < 128 && reader.TryRead(out var entry))
                {
                    batch.Add(entry);
                }

                if (batch.Count == 0)
                {
                    continue;
                }

                PersistBatch(batch);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            batch.Clear();
            while (reader.TryRead(out var entry))
            {
                batch.Add(entry);
                if (batch.Count >= 128)
                {
                    PersistBatch(batch);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                PersistBatch(batch);
            }

            FlushFallbackBuffer();
        }
    }

    private static void PersistBatch(IReadOnlyList<LogEntry> batch)
    {
        var lines = new string[batch.Count];
        for (var i = 0; i < batch.Count; i++)
        {
            lines[i] = batch[i].ToLine();
        }

        if (TryAppendLines(lines, true))
        {
            FlushFallbackBuffer();
        }
        else
        {
            foreach (var line in lines)
            {
                EnqueueFallbackLine(line);
            }
        }

        Interlocked.Add(ref _processedCount, batch.Count);
    }

    private static bool TryAppendLines(IReadOnlyList<string> lines, bool allowRotation)
    {
        if (lines.Count == 0)
        {
            return true;
        }

        try
        {
            var incomingBytes = ComputeIncomingBytes(lines);
            using (_fileLock.EnterScope())
            {
                EnsureLogDirectoryExists();
                if (allowRotation)
                {
                    RotateLogsIfNeeded(incomingBytes);
                }

                using var stream = new FileStream(LogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var writer = new StreamWriter(stream, Utf8NoBom);
                foreach (var line in lines)
                {
                    writer.WriteLine(line);
                }
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void AddUiHistory(string logLine)
    {
        var current = Application.Current;
        if (current == null)
        {
            return;
        }

        void Append()
        {
            if (LogHistory.Count >= MaxUiHistoryEntries)
            {
                LogHistory.RemoveAt(0);
            }

            LogHistory.Add(logLine);
        }

        try
        {
            if (current.Dispatcher.CheckAccess())
            {
                Append();
                return;
            }

            current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(Append));
        }
        catch
        {
        }
    }

    private static void EnqueueFallbackLine(string line)
    {
        _fallbackBuffer.Enqueue(line);
        while (_fallbackBuffer.Count > MaxFallbackBufferEntries && _fallbackBuffer.TryDequeue(out _))
        {
        }
    }

    private static void FlushFallbackBuffer()
    {
        if (_fallbackBuffer.IsEmpty)
        {
            return;
        }

        var lines = new List<string>(MaxFallbackBufferEntries);
        while (lines.Count < MaxFallbackBufferEntries && _fallbackBuffer.TryDequeue(out var line))
        {
            lines.Add(line);
        }

        if (lines.Count == 0)
        {
            return;
        }

        if (!TryAppendLines(lines, true))
        {
            foreach (var line in lines)
            {
                EnqueueFallbackLine(line);
            }
        }
    }

    private static int ComputeIncomingBytes(IReadOnlyList<string> lines)
    {
        var bytes = 0;
        foreach (var line in lines)
        {
            bytes += Utf8NoBom.GetByteCount(line);
            bytes += NewLineByteCount;
        }

        return bytes;
    }

    private static void RotateLogsIfNeeded(long incomingBytes)
    {
        if (!File.Exists(LogPath))
        {
            return;
        }

        var currentLength = new FileInfo(LogPath).Length;
        if (currentLength + incomingBytes <= MaxLogFileBytes)
        {
            return;
        }

        var oldest = $"{LogPath}.{MaxArchivedLogs}";
        if (File.Exists(oldest))
        {
            File.Delete(oldest);
        }

        for (var index = MaxArchivedLogs - 1; index >= 1; index--)
        {
            var src = $"{LogPath}.{index}";
            var dest = $"{LogPath}.{index + 1}";
            if (File.Exists(src))
            {
                File.Move(src, dest, true);
            }
        }

        File.Move(LogPath, $"{LogPath}.1", true);
    }

    private static void EnsureLogDirectoryExists()
    {
        var fullPath = Path.GetFullPath(LogPath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static string CreateSystemLine(LogLevel level, string category, string message)
    {
        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        var threadId = Environment.CurrentManagedThreadId.ToString(CultureInfo.InvariantCulture);
        return $"[{timestamp}] [T{threadId}] [{level}] [{category}] {message}";
    }

    private readonly record struct LogEntry(
        DateTimeOffset Timestamp,
        int ThreadId,
        LogLevel Level,
        string Message,
        string? Category)
    {
        public string ToLine()
        {
            var timestamp = Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
            var threadId = ThreadId.ToString(CultureInfo.InvariantCulture);
            var categoryPart = string.IsNullOrWhiteSpace(Category) ? string.Empty : $" [{Category}]";
            return $"[{timestamp}] [T{threadId}] [{Level}]{categoryPart} {Message}";
        }
    }
}
