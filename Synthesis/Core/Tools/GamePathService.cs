using System.IO;
using Microsoft.Win32;
using Synthesis.Feature.Setting;

namespace Synthesis.Core.Tools;

public static class GamePathService
{
    private const string SteamAppId = "1256670";

    // 尝试获取 BaseMod 路径
    public static string? GetBaseModPath()
    {
        // 1. 先问问 EditorConfig 里有没有存
        // 注意：EditorConfig 加载时默认为空字符串，所以要检查是否存在
        var cached = EditorConfig.Instance.BaseModPath;
        if (IsValidBaseModPath(cached)) return cached;

        // 2. 没存过，开始自动检测 (注册表 + 常见路径)
        var gameRoot = AutoDetectGamePath();
        if (!string.IsNullOrEmpty(gameRoot))
        {
            // 拼接 BaseMod 标准路径
            var baseModPath = Path.Combine(gameRoot, "LibraryOfRuina_Data", "Managed", "BaseMod");

            // 如果检测到的路径有效
            if (IsValidBaseModPath(baseModPath))
            {
                // 3. 自动存入 JSON 配置，下次直接读
                EditorConfig.Instance.BaseModPath = baseModPath;
                EditorConfig.Instance.Save();
                return baseModPath;
            }
        }

        return null;// 彻底找不到，交给 UI 弹窗让用户手动选
    }

    // 供 UI 调用：保存用户手动选择的路径
    public static void SaveUserPath(string path)
    {
        if (IsValidBaseModPath(path))
        {
            EditorConfig.Instance.BaseModPath = path;
            EditorConfig.Instance.Save();
        }
    }

    // --- 验证逻辑 ---
    private static bool IsValidBaseModPath(string? path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return false;
        // 验证特征：必须有 StaticInfo 或 Localize 文件夹
        return Directory.Exists(Path.Combine(path, "StaticInfo")) ||
               Directory.Exists(Path.Combine(path, "Localize"));
    }

    // --- 核心：多重手段寻找游戏目录 (保持不变) ---
    private static string? AutoDetectGamePath()
    {
        // A. 尝试从注册表获取
        var registryPath = GetSteamInstallPathFromRegistry();
        if (!string.IsNullOrEmpty(registryPath) && Directory.Exists(registryPath))
            return registryPath;

        // B. 暴力匹配常见路径
        var commonPaths = new[]
        {
            @"C:\Program Files (x86)\Steam\steamapps\common\Library Of Ruina",
            @"D:\SteamLibrary\steamapps\common\Library Of Ruina",
            @"E:\SteamLibrary\steamapps\common\Library Of Ruina",
            @"F:\SteamLibrary\steamapps\common\Library Of Ruina"
        };

        return commonPaths.FirstOrDefault(Directory.Exists);
    }

    private static string? GetSteamInstallPathFromRegistry()
    {
        const string subKey = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App {SteamAppId}";

        // 1. 尝试默认视图
        var path = ReadRegKey(Registry.LocalMachine, subKey);
        if (path != null) return path;

        // 2. 尝试 Wow6432Node
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(subKey);
            if (key != null) return key.GetValue("InstallLocation") as string;
        }
        catch
        {
            /* ignored */
        }

        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            using var key = baseKey.OpenSubKey(subKey);
            if (key != null) return key.GetValue("InstallLocation") as string;
        }
        catch
        {
            /* ignored */
        }

        return null;
    }

    private static string? ReadRegKey(RegistryKey root, string subKey)
    {
        try
        {
            using var key = root.OpenSubKey(subKey);
            return key?.GetValue("InstallLocation") as string;
        }
        catch
        {
            return null;
        }
    }
}