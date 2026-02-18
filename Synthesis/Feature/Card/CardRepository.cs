using System.IO;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Extensions;

namespace Synthesis.Feature.Card;

public class CardRepository : BaseRepository<UnifiedCard>
{
    public override void LoadResources(string root, string lang, string modId)
    {
        ScanAndLoad(Path.Combine(root, "StaticInfo\\Card"), "DiceCardXmlRoot", modId, AddDataDoc);
        ScanAndLoad(Path.Combine(root, "Localize\\" + lang + "\\BattlesCards"), "BattleCardDescRoot", modId, AddLocDoc);
    }

    public override void LoadLocResources(string projectRoot, string language, string modId)
    {
        ScanAndLoad(Path.Combine(projectRoot, "Localize\\" + language + "\\BattlesCards"), "BattleCardDescRoot", modId,
            AddLocDoc);
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasModData)
        {
            CreateXmlTemplate(Path.Combine(root, "StaticInfo\\Card\\CardInfo.xml"), "DiceCardXmlRoot", modId,
                AddDataDoc);
        }
        if (!HasModLoc)
        {
            CreateXmlTemplate(Path.Combine(root, "Localize\\" + lang + "\\BattlesCards\\BattlesCards.xml"),
                "BattleCardDescRoot", modId, AddLocDoc);
        }
    }

    public void DuplicateCard(UnifiedCard? sourceCard)
    {
        if (sourceCard == null)
        {
            return;
        }
        var obj = sourceCard.IsVanilla ? GetTargetDataDoc("DiceCardXmlRoot") : sourceCard.Element.Document;
        if (obj?.Root == null)
        {
            throw new Exception("未找到可写入的 CardInfo 文件");
        }
        var num = 10000000;
        if (Items.Any(x => !x.IsVanilla))
        {
            num = Items.Where(x => !x.IsVanilla).Max(x => int.TryParse(x.Id, out var result) ? result : 0) + 1;
        }
        var value = num.ToString();
        var xElement = new XElement(sourceCard.Element);
        xElement.SetAttributeValue("ID", value);
        obj.Root.Add(xElement);
        var xElement2 = GetTargetLocDoc("BattleCardDescRoot")?.Root?.Element("cardDescList");
        if (xElement2 == null)
        {
            var xElement3 = new XElement("cardDescList");
            GetTargetLocDoc("BattleCardDescRoot")?.Root?.Add(xElement3);
            xElement2 = xElement3;
        }
        var xElement4 = new XElement("BattleCardDesc", new XAttribute("ID", value));
        xElement4.Add(new XElement("LocalizedName", sourceCard.Name + " (Copy)"));
        xElement2.Add(xElement4);
        var item = new UnifiedCard(xElement, xElement4, xElement2);
        Items.Add(item);
    }

    public override void Parse(bool containOriginal)
    {
        Items.Clear();
        IEnumerable<XDocument> enumerable;
        if (!containOriginal)
        {
            IEnumerable<XDocument> modDataDocs = _modDataDocs;
            enumerable = modDataDocs;
        }
        else
        {
            enumerable = _dataDocs;
        }
        var enumerable2 = enumerable;
        IEnumerable<XDocument> source;
        if (!containOriginal)
        {
            IEnumerable<XDocument> modDataDocs = _modLocDocs;
            source = modDataDocs;
        }
        else
        {
            source = _locDocs;
        }
        var array = source.Where(d => d.Root?.Name.LocalName == "BattleCardDescRoot").ToArray();
        var textParent = GetTargetLocDoc("BattleCardDescRoot")?.Root?.Element("cardDescList") ??
                         GetTargetLocDoc("BattleCardDescRoot")?.Root;
        var dictionary = new Dictionary<string, (XElement, XElement)>();
        var array2 = array;
        foreach (var obj in array2)
        {
            var flag = obj.IsVanilla();
            foreach (var item in obj.Descendants("BattleCardDesc"))
            {
                var text = item.Attribute("ID")?.Value;
                if (!string.IsNullOrEmpty(text))
                {
                    if (!dictionary.ContainsKey(text))
                    {
                        dictionary[text] = (null, null);
                    }
                    var tuple = dictionary[text];
                    if (flag)
                    {
                        dictionary[text] = (item, tuple.Item2);
                    }
                    else
                    {
                        dictionary[text] = (tuple.Item1, item);
                    }
                }
            }
        }
        foreach (var item2 in enumerable2)
        {
            if (item2.Root?.Name.LocalName != "DiceCardXmlRoot")
            {
                continue;
            }
            var flag2 = item2.IsVanilla();
            foreach (var item3 in item2.Root.Descendants("Card"))
            {
                var text2 = item3.Attribute("ID")?.Value ?? "";
                if (string.IsNullOrEmpty(text2))
                {
                    continue;
                }
                XElement text3 = null;
                if (dictionary.TryGetValue(text2, out var value))
                {
                    object obj2;
                    if (!containOriginal)
                    {
                        obj2 = value.Item2;
                    }
                    else if (!flag2)
                    {
                        obj2 = value.Item2;
                        if (obj2 == null)
                        {
                            (obj2, _) = value;
                        }
                    }
                    else
                    {
                        obj2 = value.Item1 ?? value.Item2;
                    }
                    text3 = (XElement)obj2;
                }
                Items.Add(new UnifiedCard(item3, text3, textParent));
            }
        }
    }

    public UnifiedCard Create()
    {
        var targetDataDoc = GetTargetDataDoc("DiceCardXmlRoot");
        if (targetDataDoc == null)
        {
            throw new Exception("未找到可写入的 CardInfo 文件(非原版)");
        }
        var num = 10000000;
        if (Items.Any(x => !x.IsVanilla))
        {
            num = Items.Where(x => !x.IsVanilla).Max(x => int.TryParse(x.Id, out var result) ? result : 0) + 1;
        }
        var value = num.ToString();
        var xElement = new XElement("Card", new XAttribute("ID", value));
        xElement.Add(new XElement("Rarity", "Common"));
        xElement.Add(new XElement("Spec", new XAttribute("Cost", 0), new XAttribute("Range", "Near")));
        xElement.Add(new XElement("Script", ""));
        targetDataDoc.Root?.Add(xElement);
        var xElement2 = GetTargetLocDoc("BattleCardDescRoot")?.Root;
        if (xElement2 != null)
        {
            var xElement3 = xElement2.Element("cardDescList");
            if (xElement3 == null)
            {
                xElement3 = new XElement("cardDescList");
                xElement2.Add(xElement3);
            }
            xElement2 = xElement3;
            var xElement4 = new XElement("BattleCardDesc", new XAttribute("ID", value));
            xElement4.Add(new XElement("LocalizedName", "New Card"));
            xElement2.Add(xElement4);
            var unifiedCard = new UnifiedCard(xElement, xElement4, xElement2);
            Items.Add(unifiedCard);
            return unifiedCard;
        }
        var unifiedCard2 = new UnifiedCard(xElement, null, null);
        Items.Add(unifiedCard2);
        return unifiedCard2;
    }

    public override void Delete(UnifiedCard item)
    {
        item.DeleteXml();
        base.Delete(item);
    }
}
