using System.IO;
using System.Text.Json;
using Synthesis.Core.Tools;

namespace Synthesis.Feature.Setting;

public class EditorConfig : BindableBase
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    public static EditorConfig Instance { get; private set; } = Load();

    private static string ConfigPath => "editor_config.json";

    public bool ShowVanillaData
    {
        get;
        set => SetProperty(ref field, value);
    } = true;

    public string BaseModPath
    {
        get
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                field = GamePathService.GetBaseModPath() ?? string.Empty;
            }
            return field;
        }
        set => SetProperty(ref field, value);
    } = "";

    public string LastProjectPath
    {
        get;
        set => SetProperty(ref field, value);
    } = "";

    public string Language
    {
        get
        {
            if (string.IsNullOrWhiteSpace(field))
            {
                field = "cn";
            }
            return field;
        }
        set => SetProperty(ref field, value);
    } = "cn";

    public void Save()
    {
        try
        {
            var contents = JsonSerializer.Serialize(this, _jsonSerializerOptions);
            File.WriteAllText(ConfigPath, contents);
        }
        catch
        {
        }
    }

    private static EditorConfig Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                return JsonSerializer.Deserialize<EditorConfig>(File.ReadAllText(ConfigPath)) ?? new EditorConfig();
            }
        }
        catch
        {
        }
        return new EditorConfig();
    }
}
