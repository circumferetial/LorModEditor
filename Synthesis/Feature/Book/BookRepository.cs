using System.IO;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Extensions;

namespace Synthesis.Feature.Book;

public class BookRepository : BaseRepository<UnifiedBook>
{
    public override void LoadResources(string root, string lang, string modId)
    {
        ScanAndLoad(Path.Combine(root, "StaticInfo", "EquipPage"), "BookXmlRoot", modId, AddDataDoc);
        ScanAndLoad(Path.Combine(root, "Localize", lang, "Books"), "BookDescRoot", modId, AddLocDoc);
    }

    public override void LoadLocResources(string projectRoot, string language, string modId)
    {
        ScanAndLoad(Path.Combine(projectRoot, "Localize", language, "Books"), "BookDescRoot", modId, AddLocDoc);
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasModData)
        {
            CreateXmlTemplate(Path.Combine(root, "StaticInfo", "EquipPage", "EquipPage.xml"), "BookXmlRoot", modId,
                AddDataDoc);
        }
        if (!HasModLoc)
        {
            CreateXmlTemplate(Path.Combine(root, "Localize", lang, "Books", "Books.xml"), "BookDescRoot", modId,
                AddLocDoc);
        }
    }

    public override void Parse(bool containOriginal)
    {
        Items.Clear();
        XElement xElement = null;
        var targetLocDoc = GetTargetLocDoc("BookDescRoot");
        if (targetLocDoc != null)
        {
            xElement = targetLocDoc.Root?.Element("bookDescList") ?? targetLocDoc.Root;
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
        var array = source.Where(d => d.Root?.Name.LocalName == "BookDescRoot").ToArray();
        var dictionary = new Dictionary<(bool, string), XElement>();
        var array2 = array;
        foreach (var obj in array2)
        {
            var item = obj.IsVanilla();
            foreach (var item2 in obj.Descendants("BookDesc"))
            {
                var text = item2.Attribute("BookID")?.Value;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    dictionary.TryAdd((item, text.Trim()), item2);
                }
            }
        }
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
        foreach (var item3 in enumerable)
        {
            if (item3.Root?.Name.LocalName != "BookXmlRoot")
            {
                continue;
            }
            var flag = item3.IsVanilla();
            foreach (var item4 in item3.Root.Descendants("Book"))
            {
                var text2 = item4.Attribute("ID")?.Value ?? "";
                if (!string.IsNullOrEmpty(text2))
                {
                    var text3 = item4.Element("TextId")?.Value.Trim();
                    if (string.IsNullOrEmpty(text3))
                    {
                        text3 = text2;
                    }
                    if (!dictionary.TryGetValue((flag, text3), out var value))
                    {
                        dictionary.TryGetValue((!flag, text3), out value);
                    }
                    var textParent = flag ? null : xElement;
                    Items.Add(new UnifiedBook(item4, value, textParent));
                }
            }
        }
    }

    public void Create()
    {
        var obj = GetTargetDataDoc("BookXmlRoot") ?? throw new Exception("未找到可写入的 EquipPage 文件(非原版)");
        var num = 10000000;
        if (Items.Any(x => !x.IsVanilla))
        {
            num = Items.Where(x => !x.IsVanilla).Max(x => int.TryParse(x.Id, out var result) ? result : 0) + 1;
        }
        var text = num.ToString();
        var xElement = new XElement("Book", new XAttribute("ID", text));
        xElement.Add(new XElement("TextId", text));
        obj.Root?.Add(xElement);
        var xElement2 = GetTargetLocDoc("BookDescRoot")?.Root;
        XElement xElement3 = null;
        if (xElement2 != null)
        {
            var xElement4 = xElement2.Element("bookDescList");
            if (xElement4 == null)
            {
                xElement4 = new XElement("bookDescList");
                xElement2.Add(xElement4);
            }
            xElement2 = xElement4;
            xElement3 = new XElement("BookDesc", new XAttribute("BookID", text));
            xElement3.Add(new XElement("BookName", "New Book"));
            xElement2.Add(xElement3);
        }
        Items.Add(new UnifiedBook(xElement, xElement3, xElement2));
    }

    public override void Delete(UnifiedBook item)
    {
        item.DeleteXml();
        base.Delete(item);
    }
}
