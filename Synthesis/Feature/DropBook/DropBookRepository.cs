using System.IO;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;

namespace Synthesis.Feature.DropBook;

public class DropBookRepository : BaseRepository<UnifiedDropBook>
{
    public EtcRepository? EtcRepo { get; set; }

    public override void LoadResources(string root, string lang, string modId)
    {
        LoadDataResources(root, modId);
    }

    public override void LoadDataResources(string projectRoot, string modId)
    {
        ScanAndLoad(Path.Combine(projectRoot, "StaticInfo\\DropBook"), "BookUseXmlRoot", modId, AddDataDoc);
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasModData)
        {
            CreateXmlTemplate(Path.Combine(root, "StaticInfo\\DropBook\\DropBook.xml"), "BookUseXmlRoot", modId,
                AddDataDoc);
        }
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
        foreach (var item in enumerable)
        {
            if (item.Root?.Name.LocalName != "BookUseXmlRoot")
            {
                continue;
            }
            foreach (var item2 in item.Root.Elements("BookUse"))
            {
                Items.Add(new UnifiedDropBook(item2, EtcRepo));
            }
        }
    }

    public void Create()
    {
        var targetDataDoc = GetTargetDataDoc("BookUseXmlRoot");
        if (targetDataDoc == null)
        {
            return;
        }
        var num = 9000000;
        if (Items.Any(x => !x.IsVanilla))
        {
            num = Items.Where(x => !x.IsVanilla).Max(x => int.TryParse(x.Id, out var result) ? result : 0) + 1;
        }
        var xElement = new XElement("BookUse", new XAttribute("ID", num));
        xElement.Add(new XElement("TextId", num), new XElement("BookIcon", "FullStopOffice"),
            new XElement("Chapter", "1"));
        targetDataDoc.Root?.Add(xElement);
        Items.Add(new UnifiedDropBook(xElement, EtcRepo));
    }

    public override void Delete(UnifiedDropBook item)
    {
        item.DeleteXml();
        base.Delete(item);
    }
}
