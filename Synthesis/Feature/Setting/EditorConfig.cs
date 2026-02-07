using System.IO;
using System.Text.Json;

namespace Synthesis.Feature.Setting;

public class EditorConfig : BindableBase
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { WriteIndented = true };

    // 单例实例
    public static EditorConfig Instance { get; private set; } = Load();

    private static string ConfigPath => "editor_config.json";

    // --- 配置项 ---

    // 1. 是否加载原版数据
    public bool LoadVanillaData
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    // 2. BaseMod 路径 (缓存起来，不用每次都去注册表找)
    public string BaseModPath
    {
        get;
        set => SetProperty(ref field, value);
    } = "";

    // 3. 上次打开的项目路径 (可选优化)
    public string LastProjectPath
    {
        get;
        set => SetProperty(ref field, value);
    } = "";

    public string Language
    {
        get;
        set => SetProperty(ref field, value);
    } = "cn";

    // --- 保存与加载 ---

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, _jsonSerializerOptions);
            File.WriteAllText(ConfigPath, json);
        }
        catch
        {
            /* 忽略保存错误 */
        }
    }

    private static EditorConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<EditorConfig>(json) ?? new EditorConfig();
            }
        }
        catch
        {
            /* 忽略读取错误 */
        }

        return new EditorConfig();// 返回默认值
    }
}