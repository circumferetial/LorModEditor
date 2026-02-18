using System.IO;
using Microsoft.Win32;
using Synthesis.Feature.Setting;

namespace Synthesis.Core.Tools;

public static class GamePathService
{
    private const string SteamAppId = "1256670";

    public static string? GetBaseModPath()
    {
        var text = AutoDetectGamePath();
        if (!string.IsNullOrEmpty(text))
        {
            var text2 = Path.Combine(text, "LibraryOfRuina_Data", "Managed", "BaseMod");
            if (IsValidBaseModPath(text2))
            {
                EditorConfig.Instance.BaseModPath = text2;
                EditorConfig.Instance.Save();
                return text2;
            }
        }
        return null;
    }

    public static void SaveUserPath(string path)
    {
        if (IsValidBaseModPath(path))
        {
            EditorConfig.Instance.BaseModPath = path;
            EditorConfig.Instance.Save();
        }
    }

    private static bool IsValidBaseModPath(string? path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
        {
            return false;
        }
        if (!Directory.Exists(Path.Combine(path, "StaticInfo")))
        {
            return Directory.Exists(Path.Combine(path, "Localize"));
        }
        return true;
    }

    private static string? AutoDetectGamePath()
    {
        var steamInstallPathFromRegistry = GetSteamInstallPathFromRegistry();
        if (!string.IsNullOrEmpty(steamInstallPathFromRegistry) && Directory.Exists(steamInstallPathFromRegistry))
        {
            return steamInstallPathFromRegistry;
        }
        return new string[4]
        {
            "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Library Of Ruina",
            "D:\\SteamLibrary\\steamapps\\common\\Library Of Ruina",
            "E:\\SteamLibrary\\steamapps\\common\\Library Of Ruina",
            "F:\\SteamLibrary\\steamapps\\common\\Library Of Ruina"
        }.FirstOrDefault(Directory.Exists);
    }

    private static string? GetSteamInstallPathFromRegistry()
    {
        var text = ReadRegKey(Registry.LocalMachine,
            "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 1256670");
        if (text != null)
        {
            return text;
        }
        try
        {
            using var registryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
            using var registryKey2 =
                registryKey.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 1256670");
            if (registryKey2 != null)
            {
                return registryKey2.GetValue("InstallLocation") as string;
            }
        }
        catch
        {
        }
        try
        {
            using var registryKey3 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            using var registryKey4 =
                registryKey3.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\Steam App 1256670");
            if (registryKey4 != null)
            {
                return registryKey4.GetValue("InstallLocation") as string;
            }
        }
        catch
        {
        }
        return null;
    }

    private static string? ReadRegKey(RegistryKey root, string subKey)
    {
        try
        {
            using var registryKey = root.OpenSubKey(subKey);
            return registryKey?.GetValue("InstallLocation") as string;
        }
        catch
        {
            return null;
        }
    }
}
