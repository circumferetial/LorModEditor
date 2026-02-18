using System.IO;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Extensions;

namespace Synthesis.Feature.Passive;

public class PassiveRepository : BaseRepository<UnifiedPassive>
{
    public override void LoadResources(string root, string lang, string modId)
    {
        ScanAndLoad(Path.Combine(root, "StaticInfo\\PassiveList"), "PassiveXmlRoot", modId, AddDataDoc);
        ScanAndLoad(Path.Combine(root, "Localize\\" + lang + "\\PassiveDesc"), "PassiveDescRoot", modId, AddLocDoc);
    }

    public override void LoadLocResources(string projectRoot, string language, string modId)
    {
        ScanAndLoad(Path.Combine(projectRoot, "Localize\\" + language + "\\PassiveDesc"), "PassiveDescRoot", modId,
            AddLocDoc);
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasModData)
        {
            CreateXmlTemplate(Path.Combine(root, "StaticInfo\\PassiveList\\PassiveList.xml"), "PassiveXmlRoot", modId,
                AddDataDoc);
        }
        if (!HasModLoc)
        {
            CreateXmlTemplate(Path.Combine(root, "Localize\\" + lang + "\\PassiveDesc\\PassiveDesc.xml"),
                "PassiveDescRoot", modId, AddLocDoc);
        }
    }

    public override void Parse(bool containOriginal)
    {
        Items.Clear();
        IEnumerable<XDocument> source;
        if (!containOriginal)
        {
            IEnumerable<XDocument> modLocDocs = _modLocDocs;
            source = modLocDocs;
        }
        else
        {
            source = _locDocs;
        }
        var array = source.Where(d => d.Root?.Name.LocalName == "PassiveDescRoot").ToArray();
        IEnumerable<XDocument> enumerable;
        if (!containOriginal)
        {
            IEnumerable<XDocument> modLocDocs = _modDataDocs;
            enumerable = modLocDocs;
        }
        else
        {
            enumerable = _dataDocs;
        }
        var enumerable2 = enumerable;
        var dictionary = new Dictionary<string, XElement>();
        var dictionary2 = new Dictionary<string, XElement>();
        var array2 = array;
        foreach (var obj in array2)
        {
            var dictionary3 = obj.IsVanilla() ? dictionary : dictionary2;
            var enumerable3 = obj.Root?.Elements("PassiveDesc");
            if (enumerable3 == null)
            {
                continue;
            }
            foreach (var item in enumerable3)
            {
                var text = item.Attribute("ID")?.Value;
                if (!string.IsNullOrEmpty(text))
                {
                    dictionary3.TryAdd(text, item);
                }
            }
        }
        var textParent = GetTargetLocDoc("PassiveDescRoot")?.Root;
        foreach (var item2 in enumerable2)
        {
            if (item2.Root?.Name.LocalName != "PassiveXmlRoot")
            {
                continue;
            }
            var flag = item2.IsVanilla();
            foreach (var item3 in item2.Root.Elements("Passive"))
            {
                var text2 = item3.Attribute("ID")?.Value ?? "";
                if (string.IsNullOrEmpty(text2))
                {
                    continue;
                }
                XElement value;
                if (!containOriginal)
                {
                    dictionary2.TryGetValue(text2, out value);
                }
                else if (flag)
                {
                    if (!dictionary.TryGetValue(text2, out value))
                    {
                        dictionary2.TryGetValue(text2, out value);
                    }
                }
                else if (!dictionary2.TryGetValue(text2, out value))
                {
                    dictionary.TryGetValue(text2, out value);
                }
                Items.Add(new UnifiedPassive(item3, value, textParent));
            }
        }
    }

    public void Create()
    {
        var targetDataDoc = GetTargetDataDoc("PassiveXmlRoot");
        if (targetDataDoc == null)
        {
            throw new Exception("未找到可写入的 PassiveList 文件");
        }
        var num = 100000;
        if (Items.Any(x => !x.IsVanilla))
        {
            num = Items.Where(x => !x.IsVanilla).Max(x => int.TryParse(x.Id, out var result) ? result : 0) + 1;
        }
        var xElement = new XElement("Passive", new XAttribute("ID", num));
        xElement.Add(new XElement("Cost", 1), new XElement("Name", "New Passive"));
        targetDataDoc.Root?.Add(xElement);
        var xElement2 = GetTargetLocDoc("PassiveDescRoot")?.Root;
        XElement xElement3 = null;
        if (xElement2 != null)
        {
            xElement3 = new XElement("PassiveDesc", new XAttribute("ID", num));
            xElement3.Add(new XElement("Name", "New Passive"), new XElement("Desc", "Desc..."));
            xElement2.Add(xElement3);
        }
        Items.Add(new UnifiedPassive(xElement, xElement3, xElement2));
    }

    public override void Delete(UnifiedPassive item)
    {
        item.DeleteXml();
        base.Delete(item);
    }
}
