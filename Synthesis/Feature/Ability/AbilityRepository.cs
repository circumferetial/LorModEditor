using System.IO;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;

namespace Synthesis.Feature.Ability;

public class AbilityRepository : BaseRepository<UnifiedAbility>
{
    public override bool HasData => true;// 纯文本

    public override void LoadResources(string root, string lang, string modId)
    {
        ScanAndLoad(Path.Combine(root, "Localize", lang, "BattleCardAbilities"), "BattleCardAbilityDescRoot", modId,
            AddLocDoc);
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasLoc)
            CreateXmlTemplate(Path.Combine(root, "Localize", lang, "BattleCardAbilities", "BattleCardAbilities.xml"),
                "BattleCardAbilityDescRoot", modId, AddLocDoc);
    }

    public override void Load()
    {
        foreach (var doc in _locDocs)
        {
            if (doc.Root?.Name.LocalName != "BattleCardAbilityDescRoot") continue;
            foreach (var node in doc.Descendants("BattleCardAbility"))
            {
                if (string.IsNullOrEmpty(node.Attribute("ID")?.Value)) continue;
                // 简单去重
                if (Items.All(x => x.Id != node.Attribute("ID")?.Value))
                    Items.Add(new UnifiedAbility(node));
            }
        }
    }

    public void Create(string id = "New_Ability")
    {
        var parent = GetTargetLocDoc("BattleCardAbilityDescRoot")?.Root;
        if (parent == null) throw new Exception("未找到可写入的能力描述文件");

        var suffix = 1;
        var finalId = id;
        while (Items.Any(x => x.Id == finalId)) finalId = $"{id}_{suffix++}";

        var node = new XElement("BattleCardAbility", new XAttribute("ID", finalId));
        node.Add(new XElement("Desc", "Desc..."));
        parent.Add(node);
        Items.Add(new UnifiedAbility(node));
    }

    public override void Delete(UnifiedAbility item)
    {
        item.DeleteXml();
        base.Delete(item);
    }
}