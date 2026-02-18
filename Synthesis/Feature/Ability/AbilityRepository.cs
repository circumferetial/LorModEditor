using System.IO;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;

namespace Synthesis.Feature.Ability;

public class AbilityRepository : BaseRepository<UnifiedAbility>
{
    public override bool HasModData => true;

    public override void LoadResources(string root, string lang, string modId)
    {
        LoadLocResources(root, lang, modId);
    }

    public override void LoadLocResources(string projectRoot, string language, string modId)
    {
        ScanAndLoad(Path.Combine(projectRoot, "Localize", language, "BattleCardAbilities"), "BattleCardAbilityDescRoot",
            modId, AddLocDoc);
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasModLoc)
        {
            CreateXmlTemplate(Path.Combine(root, "Localize", lang, "BattleCardAbilities", "BattleCardAbilities.xml"),
                "BattleCardAbilityDescRoot", modId, AddLocDoc);
        }
    }

    public override void Parse(bool containOriginal)
    {
        Items.Clear();
        var hashSet = new HashSet<string>();
        IEnumerable<XDocument> enumerable;
        if (!containOriginal)
        {
            IEnumerable<XDocument> modLocDocs = _modLocDocs;
            enumerable = modLocDocs;
        }
        else
        {
            enumerable = _locDocs;
        }
        foreach (var item in enumerable)
        {
            if (item.Root?.Name.LocalName != "BattleCardAbilityDescRoot")
            {
                continue;
            }
            foreach (var item2 in item.Descendants("BattleCardAbility"))
            {
                var text = item2.Attribute("ID")?.Value;
                if (!string.IsNullOrWhiteSpace(text) && hashSet.Add(text))
                {
                    Items.Add(new UnifiedAbility(item2));
                }
            }
        }
    }

    public void Create(string id = "New_Ability")
    {
        var xElement = GetTargetLocDoc("BattleCardAbilityDescRoot")?.Root;
        if (xElement == null)
        {
            throw new Exception("未找到可写入的能力描述文件");
        }
        var num = 1;
        var finalId = id;
        while (Items.Any(x => x.Id == finalId))
        {
            finalId = $"{id}_{num++}";
        }
        var xElement2 = new XElement("BattleCardAbility", new XAttribute("ID", finalId));
        xElement2.Add(new XElement("Desc", "Desc..."));
        xElement.Add(xElement2);
        Items.Add(new UnifiedAbility(xElement2));
    }

    public override void Delete(UnifiedAbility item)
    {
        item.DeleteXml();
        base.Delete(item);
    }
}
