using System.IO;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Extensions;

namespace Synthesis.Feature.Enemy;

public class EnemyRepository : BaseRepository<UnifiedEnemy>
{
    private readonly List<XDocument> _deckDocs = [];

    private readonly List<XDocument> _unitDocs = [];

    public override bool HasModData
    {
        get
        {
            if (_unitDocs.Any(d => !d.IsVanilla()))
            {
                return _deckDocs.Any(d => !d.IsVanilla());
            }
            return false;
        }
    }

    public void AddUnitDoc(XDocument doc)
    {
        _unitDocs.Add(doc);
        NotifyStatusChanged();
    }

    public void AddDeckDoc(XDocument doc)
    {
        _deckDocs.Add(doc);
        NotifyStatusChanged();
    }

    public override void LoadResources(string root, string lang, string modId)
    {
        LoadDataResources(root, modId);
        LoadLocResources(root, lang, modId);
    }

    public override void LoadDataResources(string projectRoot, string modId)
    {
        ScanAndLoad(Path.Combine(projectRoot, "StaticInfo\\EnemyUnitInfo"), "EnemyUnitClassRoot", modId, AddUnitDoc);
        ScanAndLoad(Path.Combine(projectRoot, "StaticInfo\\Deck"), "DeckXmlRoot", modId, AddDeckDoc);
    }

    public override void LoadLocResources(string projectRoot, string language, string modId)
    {
        ScanAndLoad(Path.Combine(projectRoot, "Localize\\" + language + "\\CharactersName"), "CharactersNameRoot",
            modId, AddLocDoc);
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (_unitDocs.All(d => d.IsVanilla()))
        {
            CreateXmlTemplate(Path.Combine(root, "StaticInfo\\EnemyUnitInfo\\EnemyUnitInfo.xml"), "EnemyUnitClassRoot",
                modId, AddUnitDoc);
        }
        if (_deckDocs.All(d => d.IsVanilla()))
        {
            CreateXmlTemplate(Path.Combine(root, "StaticInfo\\Deck\\Deck.xml"), "DeckXmlRoot", modId, AddDeckDoc);
        }
        if (!HasModLoc)
        {
            CreateXmlTemplate(Path.Combine(root, "Localize\\" + lang + "\\CharactersName\\EnemyName.xml"),
                "CharactersNameRoot", modId, AddLocDoc);
        }
    }

    public override void SaveFiles(string currentModId)
    {
        SaveDocs(_unitDocs, currentModId);
        SaveDocs(_deckDocs, currentModId);
        SaveDocs(_modLocDocs, currentModId);
    }

    public override void ClearModOnly()
    {
        Items.Clear();
        _unitDocs.RemoveAll(d => !d.IsVanilla());
        _deckDocs.RemoveAll(d => !d.IsVanilla());
        _modLocDocs.Clear();
        NotifyStatusChanged();
    }

    public override void ClearAll()
    {
        Items.Clear();
        _unitDocs.Clear();
        _deckDocs.Clear();
        _modLocDocs.Clear();
        _vanillaLocDocs.Clear();
        NotifyStatusChanged();
    }

    public override void Parse(bool containOriginal)
    {
        Items.Clear();
        IEnumerable<XDocument> source;
        if (!containOriginal)
        {
            source = _unitDocs.Where(d => !d.IsVanilla());
        }
        else
        {
            IEnumerable<XDocument> unitDocs = _unitDocs;
            source = unitDocs;
        }
        var array = source.ToArray();
        IEnumerable<XDocument> source2;
        if (!containOriginal)
        {
            source2 = _deckDocs.Where(d => !d.IsVanilla());
        }
        else
        {
            IEnumerable<XDocument> unitDocs = _deckDocs;
            source2 = unitDocs;
        }
        var source3 = source2.ToArray();
        IEnumerable<XDocument> source4;
        if (!containOriginal)
        {
            IEnumerable<XDocument> unitDocs = _modLocDocs;
            source4 = unitDocs;
        }
        else
        {
            source4 = _locDocs;
        }
        var source5 = source4.Where(d => d.Root?.Name.LocalName == "CharactersNameRoot").ToArray();
        var tp = GetTargetLocDoc("CharactersNameRoot")?.Root;
        var dp = _deckDocs.FirstOrDefault(d => !d.IsVanilla())?.Root;
        var array2 = array;
        foreach (var xDocument in array2)
        {
            if (xDocument.Root?.Name.LocalName != "EnemyUnitClassRoot")
            {
                continue;
            }
            var dataIsVanilla = xDocument.IsVanilla();
            foreach (var item in xDocument.Root.Elements("Enemy"))
            {
                var id = item.Attribute("ID")?.Value.Trim() ?? "";
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }
                var nameId = item.Element("NameID")?.Value.Trim() ?? id;
                XElement xElement = null;
                foreach (var item2 in source3.Where(d => d.IsVanilla() == dataIsVanilla))
                {
                    xElement = item2.Descendants("Deck").FirstOrDefault(x => x.Attribute("ID")?.Value.Trim() == id);
                    if (xElement != null)
                    {
                        break;
                    }
                }
                if (xElement == null && containOriginal)
                {
                    foreach (var item3 in source3.Where(d => d.IsVanilla() != dataIsVanilla))
                    {
                        xElement = item3.Descendants("Deck").FirstOrDefault(x => x.Attribute("ID")?.Value.Trim() == id);
                        if (xElement != null)
                        {
                            break;
                        }
                    }
                }
                XElement xElement2 = null;
                foreach (var item4 in source5.Where(d => d.IsVanilla() == dataIsVanilla))
                {
                    xElement2 = item4.Descendants("Name")
                        .FirstOrDefault(x => x.Attribute("ID")?.Value.Trim() == nameId);
                    if (xElement2 != null)
                    {
                        break;
                    }
                }
                if (xElement2 == null && containOriginal)
                {
                    foreach (var item5 in source5.Where(d => d.IsVanilla() != dataIsVanilla))
                    {
                        xElement2 = item5.Descendants("Name")
                            .FirstOrDefault(x => x.Attribute("ID")?.Value.Trim() == nameId);
                        if (xElement2 != null)
                        {
                            break;
                        }
                    }
                }
                Items.Add(new UnifiedEnemy(item, xElement, xElement2, tp, dp));
            }
        }
    }

    public void Create()
    {
        var xDocument = _unitDocs.FirstOrDefault(d => !d.IsVanilla());
        var xDocument2 = _deckDocs.FirstOrDefault(d => !d.IsVanilla());
        if (xDocument == null || xDocument2 == null)
        {
            throw new Exception("未找到可写入的敌人数据文件或卡组文件(非原版)。\n请检查 StaticInfo 文件夹结构。");
        }
        var num = 10000000;
        if (Items.Any(x => !x.IsVanilla))
        {
            num = Items.Where(x => !x.IsVanilla).Max(x => int.Parse(x.Id)) + 1;
        }
        var text = num.ToString();
        var xElement = new XElement("Enemy", new XAttribute("ID", text));
        xElement.Add(new XElement("NameID", text));
        xElement.Add(new XElement("MinHeight", "175"));
        xElement.Add(new XElement("MaxHeight", "185"));
        xElement.Add(new XElement("BookId", ""));
        xElement.Add(new XElement("Retreat", "false"));
        xDocument.Root?.Add(xElement);
        var xElement2 = new XElement("Deck", new XAttribute("ID", text));
        xDocument2.Root?.Add(xElement2);
        var xElement3 = GetTargetLocDoc("CharactersNameRoot")?.Root;
        XElement xElement4 = null;
        if (xElement3 != null)
        {
            xElement4 = new XElement("Name", new XAttribute("ID", text), "New Enemy");
            xElement3.Add(xElement4);
        }
        Items.Add(new UnifiedEnemy(xElement, xElement2, xElement4, xElement3, xDocument2.Root));
    }

    public override void Delete(UnifiedEnemy item)
    {
        item.DeleteXml();
        base.Delete(item);
    }
}
