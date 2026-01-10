using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace LorModEditor.Core.Log;

public static class Logger
{
    private const string LogPath = "latest.log";
    private static readonly Lock _lock = new();

    // 静态构造函数：每次启动时清空旧日志
    static Logger()
    {
        try
        {
            File.WriteAllText(LogPath, $"=== Session Started: {DateTime.Now} ===\n");
        }
        catch
        {
            /* 忽略文件占用错误 */
        }
    }

    // 供 UI 绑定的实时日志列表
    public static ObservableCollection<string> LogHistory { get; } = [];

    public static void Log(string message, LogLevel level = LogLevel.Info)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var logLine = $"[{timestamp}] [{level}] {message}";

        // 1. 写入文件 (加锁防止多线程冲突)
        lock (_lock)
        {
            try
            {
                File.AppendAllText(LogPath, logLine + Environment.NewLine);
            }
            catch
            {
                /* 写文件失败别崩了主程序 */
            }
        }

        // 2. 更新 UI (必须在主线程)
        Application.Current?.Dispatcher.Invoke(() =>
        {
            // 保持列表不过长，只留最后 200 行
            if (LogHistory.Count > 200) LogHistory.RemoveAt(0);
            LogHistory.Add(logLine);
        });
    }

    // 快捷方法
    public static void Info(string msg) => Log(msg);
    public static void Warn(string msg) => Log(msg, LogLevel.Warning);

    public static void Error(string msg, Exception? ex = null)
    {
        if (ex != null)
        {
            msg += $"\n   >>> Exception: {ex.GetType().Name}: {ex.Message}";
            msg += $"\n   >>> StackTrace: {ex.StackTrace}";
        }
        Log(msg, LogLevel.Error);
    }
}