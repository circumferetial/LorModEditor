using System.IO;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Extensions;

namespace Synthesis.Feature.Stage;

public class StageRepository : BaseRepository<UnifiedStage>
{
    public override void LoadResources(string root, string lang, string modId)
    {
        ScanAndLoad(Path.Combine(root, "StaticInfo\\StageInfo"), "StageXmlRoot", modId, AddDataDoc);
        ScanAndLoad(Path.Combine(root, "Localize\\" + lang + "\\StageName"), "CharactersNameRoot", modId, AddLocDoc);
    }

    public override void LoadLocResources(string projectRoot, string language, string modId)
    {
        ScanAndLoad(Path.Combine(projectRoot, "Localize\\" + language + "\\StageName"), "CharactersNameRoot", modId,
            AddLocDoc);
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasModData)
        {
            CreateXmlTemplate(Path.Combine(root, "StaticInfo\\StageInfo\\StageInfo.xml"), "StageXmlRoot", modId,
                AddDataDoc);
        }
        if (!HasModLoc)
        {
            CreateXmlTemplate(Path.Combine(root, "Localize\\" + lang + "\\StageName\\StageName.xml"),
                "CharactersNameRoot", modId, AddLocDoc);
        }
    }

    public override void Parse(bool containOriginal)
    {
        Items.Clear();
        IEnumerable<XDocument> enumerable;
        if (!containOriginal)
        {
            IEnumerable<XDocument> modLocDocs = _modLocDocs;
            enumerable = modLocDocs;
        }
        else
        {
            enumerable = _locDocs;
        }
        IEnumerable<XDocument> enumerable2;
        if (!containOriginal)
        {
            IEnumerable<XDocument> modLocDocs = _modDataDocs;
            enumerable2 = modLocDocs;
        }
        else
        {
            enumerable2 = _dataDocs;
        }
        var enumerable3 = enumerable2;
        var textParent = GetTargetLocDoc("CharactersNameRoot")?.Root;
        var dictionary = new Dictionary<(bool, string), XElement>();
        foreach (var item2 in enumerable)
        {
            if (item2.Root?.Name.LocalName != "CharactersNameRoot")
            {
                continue;
            }
            var item = item2.IsVanilla();
            foreach (var item3 in item2.Descendants("Name"))
            {
                var text = item3.Attribute("ID")?.Value.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    (bool, string) key = (item, text);
                    dictionary.TryAdd(key, item3);
                }
            }
        }
        foreach (var item4 in enumerable3)
        {
            if (item4.Root?.Name.LocalName != "StageXmlRoot")
            {
                continue;
            }
            var flag = item4.IsVanilla();
            foreach (var item5 in item4.Root.Elements("Stage"))
            {
                var text2 = item5.Attribute("id")?.Value.Trim() ?? "";
                if (!string.IsNullOrEmpty(text2))
                {
                    XElement value;
                    if (!containOriginal)
                    {
                        dictionary.TryGetValue((false, text2), out value);
                    }
                    else if (!dictionary.TryGetValue((flag, text2), out value))
                    {
                        dictionary.TryGetValue((!flag, text2), out value);
                    }
                    Items.Add(new UnifiedStage(item5, value, textParent));
                }
            }
        }
    }

    public void Create()
    {
        var targetDataDoc = GetTargetDataDoc("StageXmlRoot");
        if (targetDataDoc == null)
        {
            throw new Exception("未找到可写入的 StageInfo 文件(非原版)");
        }
        var num = 100000;
        if (Items.Any(x => !x.IsVanilla))
        {
            num = Items.Where(x => !x.IsVanilla).Max(x => int.TryParse(x.Id, out var result) ? result : 0) + 1;
        }
        var xElement = new XElement("Stage", new XAttribute("id", num));
        var xElement2 = new XElement("Wave");
        xElement2.Add(new XElement("Formation", "1"), new XElement("AvailableUnit", "5"));
        xElement.Add(xElement2);
        targetDataDoc.Root?.Add(xElement);
        var xElement3 = GetTargetLocDoc("CharactersNameRoot")?.Root;
        XElement xElement4 = null;
        if (xElement3 != null)
        {
            xElement4 = new XElement("Name", new XAttribute("ID", num), "New Stage");
            xElement3.Add(xElement4);
        }
        Items.Add(new UnifiedStage(xElement, xElement4, xElement3));
    }

    public override void Delete(UnifiedStage item)
    {
        item.DeleteXml();
        base.Delete(item);
    }
}
