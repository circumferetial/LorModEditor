using System.IO;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Extensions;
using Synthesis.Core.Tools;

namespace Synthesis.Feature.DropBook;

public class EtcRepository : IGameRepository
{
    private readonly List<XDocument> _modDocs = [];

    private readonly List<XDocument> _vanillaDocs = [];

    public Dictionary<string, string> VanillaTextCache { get; } = new();

    public Dictionary<string, string> ModTextCache { get; } = new();

    public bool HasLoc => _modDocs.Count > 0;

    public void ClearLocOnly()
    {
        _modDocs.Clear();
        _vanillaDocs.Clear();
        VanillaTextCache.Clear();
        ModTextCache.Clear();
    }

    public void ClearModOnly()
    {
        _modDocs.Clear();
        ModTextCache.Clear();
    }

    public void ClearAll()
    {
        _modDocs.Clear();
        _vanillaDocs.Clear();
        VanillaTextCache.Clear();
        ModTextCache.Clear();
    }

    public void LoadResources(string root, string lang, string modId)
    {
        LoadLocResources(root, lang, modId);
    }

    public void LoadLocResources(string projectRoot, string language, string modId)
    {
        var path = Path.Combine(projectRoot, "Localize\\" + language + "\\etc");
        if (!Directory.Exists(path))
        {
            return;
        }
        foreach (var item in Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly).Where(
                     delegate(string f)
                     {
                         var extension = Path.GetExtension(f);
                         return extension == ".txt" || extension == ".xml" ? true : false;
                     }))
        {
            try
            {
                var xDocument = XDocument.Load(item);
                if (!(xDocument.Root?.Name.LocalName != "localize"))
                {
                    xDocument.Root.AddAnnotation("PID:" + modId);
                    xDocument.Root.AddAnnotation(new FilePathAnnotation(item));
                    if (xDocument.IsVanilla())
                    {
                        _vanillaDocs.Add(xDocument);
                    }
                    else
                    {
                        _modDocs.Add(xDocument);
                    }
                }
            }
            catch
            {
            }
        }
    }

    public void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasLoc)
        {
            var text = Path.Combine(root, "Localize\\" + lang + "\\etc\\DropBookNames.xml");
            var directoryName = Path.GetDirectoryName(text);
            if (!Directory.Exists(directoryName))
            {
                Directory.CreateDirectory(directoryName);
            }
            if (!File.Exists(text))
            {
                var xDocument = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("localize"));
                xDocument.Root?.AddAnnotation("PID:" + modId);
                xDocument.Root?.AddAnnotation(new FilePathAnnotation(text));
                xDocument.Save(text);
                _modDocs.Add(xDocument);
            }
        }
    }

    public void Parse(bool containOriginal)
    {
        VanillaTextCache.Clear();
        ModTextCache.Clear();
        if (containOriginal)
        {
            foreach (var vanillaDoc in _vanillaDocs)
            {
                if (vanillaDoc.Root?.Name.LocalName != "localize")
                {
                    continue;
                }
                foreach (var item in vanillaDoc.Root.Elements("text"))
                {
                    var text = item.Attribute("id")?.Value;
                    if (!string.IsNullOrEmpty(text))
                    {
                        VanillaTextCache[text] = item.Value;
                    }
                }
            }
        }
        foreach (var modDoc in _modDocs)
        {
            if (modDoc.Root?.Name.LocalName != "localize")
            {
                continue;
            }
            foreach (var item2 in modDoc.Root.Elements("text"))
            {
                var text2 = item2.Attribute("id")?.Value;
                if (!string.IsNullOrEmpty(text2))
                {
                    ModTextCache[text2] = item2.Value;
                }
            }
        }
    }

    public void SaveFiles(string modId)
    {
        foreach (var modDoc in _modDocs)
        {
            if (!(modDoc.GetPackageId() != modId))
            {
                var text = modDoc.Root?.Annotation<FilePathAnnotation>()?.Path;
                if (!string.IsNullOrEmpty(text))
                {
                    modDoc.Save(text);
                }
            }
        }
    }

    public string? GetText(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }
        if (!ModTextCache.TryGetValue(id, out var value))
        {
            return VanillaTextCache.GetValueOrDefault(id);
        }
        return value;
    }

    public void SetText(string id, string value)
    {
        if (string.IsNullOrEmpty(id))
        {
            return;
        }
        ModTextCache[id] = value;
        foreach (var modDoc in _modDocs)
        {
            var xElement = modDoc.Root?.Elements("text").FirstOrDefault(x => x.Attribute("id")?.Value == id);
            if (xElement != null)
            {
                xElement.Value = value;
                return;
            }
        }
        _modDocs.FirstOrDefault()?.Root?.Add(new XElement("text", new XAttribute("id", id), value));
    }
}
