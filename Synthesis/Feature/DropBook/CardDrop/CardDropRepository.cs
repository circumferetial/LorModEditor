using System.IO;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;

namespace Synthesis.Feature.DropBook.CardDrop;

public class CardDropRepository : BaseRepository<UnifiedCardDrop>
{
    public override void LoadResources(string root, string lang, string modId)
    {
        LoadDataResources(root, modId);
    }

    public override void LoadDataResources(string projectRoot, string modId)
    {
        ScanAndLoad(Path.Combine(projectRoot, "StaticInfo\\CardDropTable"), "CardDropTableXmlRoot", modId, AddDataDoc);
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasModData)
        {
            CreateXmlTemplate(Path.Combine(root, "StaticInfo\\CardDropTable\\CardDropTable.xml"),
                "CardDropTableXmlRoot", modId, AddDataDoc);
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
            if (item.Root?.Name.LocalName != "CardDropTableXmlRoot")
            {
                continue;
            }
            foreach (var item2 in item.Root.Elements("DropTable"))
            {
                Items.Add(new UnifiedCardDrop(item2));
            }
        }
    }

    public UnifiedCardDrop? GetByBookId(string bookId)
    {
        return Items.FirstOrDefault(x => x.BookId == bookId && !x.IsVanilla) ??
               Items.FirstOrDefault(x => x.BookId == bookId);
    }

    public UnifiedCardDrop Create(string bookId)
    {
        var obj = GetTargetDataDoc("CardDropTableXmlRoot") ?? throw new Exception("未找到可写入的 CardDropTable 文件");
        var xElement = new XElement("DropTable", new XAttribute("ID", bookId));
        obj.Root?.Add(xElement);
        var unifiedCardDrop = new UnifiedCardDrop(xElement);
        Items.Add(unifiedCardDrop);
        return unifiedCardDrop;
    }
}
