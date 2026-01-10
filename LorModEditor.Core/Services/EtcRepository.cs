using System.IO;
using System.Xml.Linq;
using LorModEditor.Core.Extension;

namespace LorModEditor.Core.Services;

public class EtcRepository
{
    private readonly List<XDocument> _docs = new();
    public Dictionary<string, string> TextCache { get; } = new();

    public bool HasLoc => _docs.Any(doc => !doc.IsVanilla());

    public void Clear()
    {
        _docs.Clear();
        TextCache.Clear();
    }

    public void LoadResources(string root, string lang, string modId)
    {
        if (Directory.Exists(Path.Combine(root, $@"Localize\{lang}\etc")))
        {
            // 这里的根节点是 localize
            // 简单起见，我们不使用 BaseRepository 的 ScanAndLoad，而是手动扫描
            var files = Directory.GetFiles(Path.Combine(root, $@"Localize\{lang}\etc"), "*.xml");
            foreach (var file in files)
            {
                try
                {
                    var doc = XDocument.Load(file);
                    if (doc.Root?.Name.LocalName == "localize")
                    {
                        doc.Root.AddAnnotation("PID:" + modId);
                        doc.Root.AddAnnotation(new FilePathAnnotation(file));
                        _docs.Add(doc);
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }
    }

    public void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasLoc)
        {
            // 手动创建
            var path = Path.Combine(root, $@"Localize\{lang}\etc\DropBookNames.xml");
            var dir = Path.GetDirectoryName(path)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            if (!File.Exists(path))
            {
                var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("localize"));
                doc.Root?.AddAnnotation("PID:" + modId);
                doc.Save(path);
                _docs.Add(doc);
            }
        }
    }

    public void Load()
    {
        foreach (var doc in _docs)
        {
            if (doc.Root?.Name.LocalName != "localize") continue;
            foreach (var node in doc.Root.Elements("text"))
            {
                var id = node.Attribute("id")?.Value;
                if (!string.IsNullOrEmpty(id)) TextCache[id] = node.Value;
            }
        }
    }

// 修改 GetText 方法
    public string? GetText(string id) // 返回可空字符串
    {
        return string.IsNullOrEmpty(id) ? null : TextCache.GetValueOrDefault(id);// 没找到返回 null
    }

    public void SetText(string id, string value)
    {
        if (string.IsNullOrEmpty(id)) return;
        TextCache[id] = value;

        // 写入
        foreach (var doc in _docs)
        {
            if (doc.IsVanilla()) continue;
            var existing = doc.Root?.Elements("text").FirstOrDefault(x => x.Attribute("id")?.Value == id);
            if (existing != null)
            {
                existing.Value = value;
                return;
            }
        }

        var target = _docs.FirstOrDefault(d => !d.IsVanilla());
        target?.Root?.Add(new XElement("text", new XAttribute("id", id), value));
    }

    public void SaveFiles(string modId)
    {
        foreach (var doc in _docs)
        {
            if (doc.GetPackageId() == modId)
            {
                var path = doc.Root?.Annotation<FilePathAnnotation>()?.Path;
                if (!string.IsNullOrEmpty(path)) doc.Save(path);
            }
        }
    }
}