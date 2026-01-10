using System.IO;
using System.Xml.Linq;
using LorModEditor.Core.Extension;
using LorModEditor.Core.Wrappers;

namespace LorModEditor.Core.Services;

public class KeywordRepository : BaseRepository<UnifiedKeyword>
{
    public override bool HasData => true;// 纯文本仓库，逻辑上总是有数据的

    public override void LoadResources(string root, string lang, string modId)
    {
        ScanAndLoad(Path.Combine(root, $@"Localize\{lang}\EffectTexts"), "BattleEffectTextRoot", modId, AddLocDoc);
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasLoc)
            CreateXmlTemplate(Path.Combine(root, $@"Localize\{lang}\EffectTexts\Keywords.xml"), "BattleEffectTextRoot",
                modId, AddLocDoc);
    }

    public override void Load()
    {
        // 准备默认的 Mod 挂载点
        XElement? modParent = null;
        var modDoc = GetTargetLocDoc("BattleEffectTextRoot");
        if (modDoc != null)
        {
            modParent = modDoc.Root?.Element("effectTextList") ?? modDoc.Root;
        }

        foreach (var doc in _locDocs)
        {
            if (doc.Root?.Name.LocalName != "BattleEffectTextRoot") continue;

            // 【核心修复】兼容读取：有 list 读 list，没 list 读 root
            var listNode = doc.Root.Element("effectTextList") ?? doc.Root;

            foreach (var node in listNode.Elements("BattleEffectText"))
            {
                // 如果是原版，parent=null；如果是 Mod，parent=modParent
                var parent = doc.IsVanilla() ? null : modParent;
                Items.Add(new UnifiedKeyword(node, parent));
            }
        }
    }

    public void Create()
    {
        var doc = GetTargetLocDoc("BattleEffectTextRoot");
        if (doc == null) throw new Exception("缺少 Keywords 文件(非原版)");

        // 【核心修复】确保 effectTextList 存在
        var parent = doc.Root;
        if (parent != null)
        {
            var listNode = parent.Element("effectTextList");
            if (listNode == null)
            {
                listNode = new XElement("effectTextList");
                parent.Add(listNode);
            }
            parent = listNode;
        }

        // 计算 ID
        var newId = "New_Keyword";
        var suffix = 1;
        while (Items.Any(x => x.Id == newId)) newId = $"New_Keyword_{suffix++}";

        var node = new XElement("BattleEffectText", new XAttribute("ID", newId));
        node.Add(new XElement("Name", "Name"), new XElement("Desc", "Desc"));

        parent?.Add(node);
        Items.Add(new UnifiedKeyword(node, parent));
    }

    public override void Delete(UnifiedKeyword item)
    {
        item.DeleteXml();
        base.Delete(item);
    }
}