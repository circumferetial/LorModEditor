using System.IO;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Extensions;

namespace Synthesis.Feature.Keyword;

public class KeywordRepository : BaseRepository<UnifiedKeyword>
{
    public override bool HasModData => true;

    public override void LoadResources(string root, string lang, string modId)
    {
        LoadLocResources(root, lang, modId);
    }

    public override void LoadLocResources(string projectRoot, string language, string modId)
    {
        ScanAndLoad(Path.Combine(projectRoot, "Localize", language, "EffectTexts"), "BattleEffectTextRoot", modId,
            AddLocDoc);
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasModLoc)
        {
            CreateXmlTemplate(Path.Combine(root, "Localize", lang, "EffectTexts", "Keywords.xml"),
                "BattleEffectTextRoot", modId, AddLocDoc);
        }
    }

    public override void Parse(bool containOriginal)
    {
        Items.Clear();
        XElement xElement = null;
        var targetLocDoc = GetTargetLocDoc("BattleEffectTextRoot");
        if (targetLocDoc?.Root != null)
        {
            xElement = targetLocDoc.Root.Element("effectTextList") ?? targetLocDoc.Root;
        }
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
        var array = source.Where(d => d.Root?.Name.LocalName == "BattleEffectTextRoot").ToArray();
        foreach (var xDocument in array)
        {
            var root = xDocument.Root;
            if (root == null)
            {
                continue;
            }
            var obj = root.Element("effectTextList") ?? root;
            var parent = xDocument.IsVanilla() ? null : xElement;
            foreach (var item in obj.Elements("BattleEffectText"))
            {
                Items.Add(new UnifiedKeyword(item, parent));
            }
        }
    }

    public void Create()
    {
        var xElement = (GetTargetLocDoc("BattleEffectTextRoot") ?? throw new Exception("缺少 Keywords 文件(非原版)")).Root;
        if (xElement != null)
        {
            var xElement2 = xElement.Element("effectTextList");
            if (xElement2 == null)
            {
                xElement2 = new XElement("effectTextList");
                xElement.Add(xElement2);
            }
            xElement = xElement2;
        }
        var newId = "New_Keyword";
        var num = 1;
        while (Items.Any(x => x.Id == newId))
        {
            newId = $"New_Keyword_{num++}";
        }
        var xElement3 = new XElement("BattleEffectText", new XAttribute("ID", newId));
        xElement3.Add(new XElement("Name", "Name"), new XElement("Desc", "Desc"));
        xElement?.Add(xElement3);
        Items.Add(new UnifiedKeyword(xElement3, xElement));
    }

    public override void Delete(UnifiedKeyword item)
    {
        item.DeleteXml();
        base.Delete(item);
    }
}
