using System.IO;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Extensions;

namespace Synthesis.Feature.Book;

public class BookRepository : BaseRepository<UnifiedBook>
{
    public override void LoadResources(string root, string lang, string modId)
    {
        ScanAndLoad(Path.Combine(root, @"StaticInfo\EquipPage"), "BookXmlRoot", modId, AddDataDoc);
        ScanAndLoad(Path.Combine(root, $@"Localize\{lang}\Books"), "BookDescRoot", modId, AddLocDoc);
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasData)
            CreateXmlTemplate(Path.Combine(root, @"StaticInfo\EquipPage\EquipPage.xml"), "BookXmlRoot", modId,
                AddDataDoc);

        if (!HasLoc)
            CreateXmlTemplate(Path.Combine(root, $@"Localize\{lang}\Books\Books.xml"), "BookDescRoot", modId,
                AddLocDoc);
    }

    public override void Load()
    {
        // 准备默认的挂载点 (优先找 bookDescList)
        XElement? modLocParent = null;
        var modDoc = GetTargetLocDoc("BookDescRoot");
        if (modDoc != null)
        {
            modLocParent = modDoc.Root?.Element("bookDescList") ?? modDoc.Root;
        }

        foreach (var doc in _dataDocs)
        {
            if (doc.Root?.Name.LocalName != "BookXmlRoot") continue;

            foreach (var node in doc.Root.Elements("Book"))
            {
                var id = node.Attribute("ID")?.Value ?? "";
                if (string.IsNullOrEmpty(id)) continue;
                var nameId = node.Element("TextId")?.Value.Trim() ?? id;

                XElement? foundText = null;
                var isVanilla = doc.IsVanilla();

                // 查找翻译
                var targetLocs = _locDocs.Where(d => d.Root?.Name.LocalName == "BookDescRoot").ToArray();
                foreach (var loc in targetLocs)
                {
                    if (loc.IsVanilla() == isVanilla)
                    {
                        foundText = loc.Descendants("BookDesc")
                            .FirstOrDefault(x => x.Attribute("BookID")?.Value == nameId);
                        if (foundText != null) break;
                    }
                }
                if (foundText == null)
                {
                    foreach (var loc in targetLocs)
                    {
                        if (loc.IsVanilla() != isVanilla)
                        {
                            foundText = loc.Descendants("BookDesc")
                                .FirstOrDefault(x => x.Attribute("BookID")?.Value == nameId);
                            if (foundText != null) break;
                        }
                    }
                }

                var parent = isVanilla ? null : modLocParent;
                Items.Add(new UnifiedBook(node, foundText, parent));
            }
        }
    }

    public void Create()
    {
        var targetDoc = GetTargetDataDoc("BookXmlRoot");
        if (targetDoc == null) throw new Exception("未找到可写入的 EquipPage 文件(非原版)");

        var newId = 10000000;
        if (Items.Any(x => !x.IsVanilla))
            newId = Items.Where(x => !x.IsVanilla).Max(x => int.TryParse(x.Id, out var i) ? i : 0) + 1;
        var strId = newId.ToString();

        // 1. 创建数据
        var node = new XElement("Book", new XAttribute("ID", strId));
        node.Add(new XElement("TextId", strId));
        targetDoc.Root?.Add(node);

        // 2. 创建翻译
        var locDoc = GetTargetLocDoc("BookDescRoot");
        var parent = locDoc?.Root;
        XElement? text = null;

        if (parent != null)
        {
            // 【核心修复】检查 bookDescList 层级
            var listNode = parent.Element("bookDescList");
            if (listNode == null)
            {
                listNode = new XElement("bookDescList");
                parent.Add(listNode);
            }
            parent = listNode;

            text = new XElement("BookDesc", new XAttribute("BookID", strId));
            text.Add(new XElement("BookName", "New Book"));
            parent.Add(text);
        }

        // 3. 添加到 UI
        Items.Add(new UnifiedBook(node, text, parent));
    }

    public override void Delete(UnifiedBook item)
    {
        item.DeleteXml();
        base.Delete(item);
    }
}