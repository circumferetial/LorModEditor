using System.IO;
using System.Xml.Linq;
using LorModEditor.Core.Wrappers;

namespace LorModEditor.Core.Services;

public class DropBookRepository : BaseRepository<UnifiedDropBook>
{
    // 依赖注入 EtcRepo 用于获取名字
    public EtcRepository? EtcRepo { get; set; }

    public override void LoadResources(string root, string lang, string modId)
    {
        ScanAndLoad(Path.Combine(root, @"StaticInfo\DropBook"), "BookUseXmlRoot", modId, AddDataDoc);
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasData)
            CreateXmlTemplate(Path.Combine(root, @"StaticInfo\DropBook\DropBook.xml"), "BookUseXmlRoot", modId,
                AddDataDoc);
    }

    public override void Load()
    {
        foreach (var doc in _dataDocs)
        {
            if (doc.Root?.Name.LocalName != "BookUseXmlRoot") continue;
            foreach (var node in doc.Root.Elements("BookUse"))
            {
                Items.Add(new UnifiedDropBook(node, EtcRepo!));
            }
        }
    }

    public void Create()
    {
        var targetDoc = GetTargetDataDoc("BookUseXmlRoot");
        if (targetDoc == null) throw new Exception("未找到可写入的 DropBook 文件(非原版)");

        var newId = 9000000;
        if (Items.Any(x => !x.IsVanilla))
            newId = Items.Where(x => !x.IsVanilla).Max(x => int.TryParse(x.Id, out var i) ? i : 0) + 1;

        var node = new XElement("BookUse", new XAttribute("ID", newId));
        node.Add(new XElement("TextId", newId), new XElement("BookIcon", "FullStopOffice"),
            new XElement("Chapter", "1"));
        targetDoc.Root?.Add(node);

        Items.Add(new UnifiedDropBook(node, EtcRepo!));
    }

    public override void Delete(UnifiedDropBook item)
    {
        item.DeleteXml();
        base.Delete(item);
    }
}