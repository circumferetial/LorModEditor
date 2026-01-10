using System.IO;
using System.Xml.Linq;
using LorModEditor.Core.Wrappers;

namespace LorModEditor.Core.Services;

public class PassiveRepository : BaseRepository<UnifiedPassive>
{
    public override void LoadResources(string root, string lang, string modId)
    {
        ScanAndLoad(Path.Combine(root, @"StaticInfo\PassiveList"), "PassiveXmlRoot", modId, AddDataDoc);
        ScanAndLoad(Path.Combine(root, $@"Localize\{lang}\PassiveDesc"), "PassiveDescRoot", modId, AddLocDoc);
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasData)
            CreateXmlTemplate(Path.Combine(root, @"StaticInfo\PassiveList\PassiveList.xml"), "PassiveXmlRoot", modId,
                AddDataDoc);
        if (!HasLoc)
            CreateXmlTemplate(Path.Combine(root, $@"Localize\{lang}\PassiveDesc\PassiveDesc.xml"), "PassiveDescRoot",
                modId, AddLocDoc);
    }

    public override void Load()
    {
        var modParent = GetTargetLocDoc("PassiveDescRoot")?.Root;
        foreach (var doc in _dataDocs)
        {
            if (doc.Root?.Name.LocalName != "PassiveXmlRoot") continue;
            foreach (var node in doc.Root.Elements("Passive"))
            {
                var id = node.Attribute("ID")?.Value ?? "";
                if (string.IsNullOrEmpty(id)) continue;
                XElement? foundText = null;
                foreach (var loc in _locDocs.Where(d => d.Root?.Name.LocalName == "PassiveDescRoot"))
                {
                    foundText = loc.Descendants("PassiveDesc").FirstOrDefault(x => x.Attribute("ID")?.Value == id);
                    if (foundText != null) break;
                }
                Items.Add(new UnifiedPassive(node, foundText, modParent));
            }
        }
    }

    // ... Create / Delete 参考 CardRepo ...
    // (Create 方法在之前的回复中已经提供过完整的，请直接使用)
    public void Create()
    {
        var targetDoc = GetTargetDataDoc("PassiveXmlRoot");
        if (targetDoc == null) throw new Exception("未找到可写入的 PassiveList 文件");

        var newId = 100000;
        if (Items.Any(x => !x.IsVanilla))
            newId = Items.Where(x => !x.IsVanilla).Max(x => int.TryParse(x.Id, out var i) ? i : 0) + 1;

        var node = new XElement("Passive", new XAttribute("ID", newId));
        node.Add(new XElement("Cost", 1), new XElement("Name", "New Passive"));
        targetDoc.Root?.Add(node);

        var parent = GetTargetLocDoc("PassiveDescRoot")?.Root;
        XElement? text = null;
        if (parent != null)
        {
            text = new XElement("PassiveDesc", new XAttribute("ID", newId));
            text.Add(new XElement("Name", "New Passive"), new XElement("Desc", "Desc..."));
            parent.Add(text);
        }
        Items.Add(new UnifiedPassive(node, text, parent));
    }

    public override void Delete(UnifiedPassive item)
    {
        item.DeleteXml();
        base.Delete(item);
    }
}