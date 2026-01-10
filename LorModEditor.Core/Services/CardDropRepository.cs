using System.IO;
using System.Xml.Linq;
using LorModEditor.Core.Wrappers;

namespace LorModEditor.Core.Services;

public class CardDropRepository : BaseRepository<UnifiedCardDrop>
{
    public override void LoadResources(string root, string lang, string modId)
    {
        ScanAndLoad(Path.Combine(root, @"StaticInfo\CardDropTable"), "CardDropTableXmlRoot", modId, AddDataDoc);
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasData)
            CreateXmlTemplate(Path.Combine(root, @"StaticInfo\CardDropTable\CardDropTable.xml"), "CardDropTableXmlRoot",
                modId, AddDataDoc);
    }

    public override void Load()
    {
        foreach (var node in _dataDocs.Where(doc => doc.Root?.Name.LocalName == "CardDropTableXmlRoot")
                     .SelectMany(doc => doc.Root?.Elements("DropTable") ?? []))
        {
            Items.Add(new UnifiedCardDrop(node));
        }
    }

    public UnifiedCardDrop? GetByBookId(string bookId) =>
        Items.FirstOrDefault(x => x.BookId == bookId && !x.IsVanilla) ?? Items.FirstOrDefault(x => x.BookId == bookId);

    public UnifiedCardDrop Create(string bookId)
    {
        var targetDoc = GetTargetDataDoc("CardDropTableXmlRoot");
        if (targetDoc == null) throw new Exception("未找到可写入的 CardDropTable 文件");

        var node = new XElement("DropTable", new XAttribute("ID", bookId));
        targetDoc.Root?.Add(node);
        var item = new UnifiedCardDrop(node);
        Items.Add(item);
        return item;
    }
}