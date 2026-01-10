using System.IO;
using System.Xml.Linq;
using LorModEditor.Core.Wrappers;

namespace LorModEditor.Core.Services;

public class StageRepository : BaseRepository<UnifiedStage>
{
    public override void LoadResources(string root, string lang, string modId)
    {
        ScanAndLoad(Path.Combine(root, @"StaticInfo\StageInfo"), "StageXmlRoot", modId, AddDataDoc);
        // 关卡名字必须在 StageName 文件夹下
        ScanAndLoad(Path.Combine(root, $@"Localize\{lang}\StageName"), "CharactersNameRoot", modId, AddLocDoc);
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasData)
            CreateXmlTemplate(Path.Combine(root, @"StaticInfo\StageInfo\StageInfo.xml"), "StageXmlRoot", modId,
                AddDataDoc);
        if (!HasLoc)
            CreateXmlTemplate(Path.Combine(root, $@"Localize\{lang}\StageName\StageName.xml"), "CharactersNameRoot",
                modId, AddLocDoc);
    }

    public override void Load()
    {
        foreach (var doc in _dataDocs)
        {
            if (doc.Root?.Name.LocalName != "StageXmlRoot") continue;

            foreach (var node in doc.Root.Elements("Stage"))
            {
                var id = node.Attribute("id")?.Value.Trim() ?? "";// 【修复】加 Trim()
                if (string.IsNullOrEmpty(id)) continue;

                XElement? foundText = null;
                foreach (var loc in _locDocs)
                {
                    if (loc.Root?.Name.LocalName == "CharactersNameRoot")
                    {
                        foundText = loc.Descendants("Name")
                            .FirstOrDefault(x => x.Attribute("ID")?.Value?.Trim() == id);// 【修复】加 Trim()
                        if (foundText != null) break;
                    }
                }

                var parent = GetTargetLocDoc("CharactersNameRoot")?.Root;
                Items.Add(new UnifiedStage(node, foundText, parent));
            }
        }
    }

    public void Create()
    {
        var targetDoc = GetTargetDataDoc("StageXmlRoot");
        if (targetDoc == null) throw new Exception("未找到可写入的 StageInfo 文件(非原版)");

        var newId = 100000;
        if (Items.Any(x => !x.IsVanilla))
            newId = Items.Where(x => !x.IsVanilla).Max(x => int.TryParse(x.Id, out var i) ? i : 0) + 1;

        var node = new XElement("Stage", new XAttribute("id", newId));
        var wave = new XElement("Wave");
        wave.Add(new XElement("Formation", "1"), new XElement("AvailableUnit", "5"));
        node.Add(wave);
        targetDoc.Root?.Add(node);

        var parent = GetTargetLocDoc("CharactersNameRoot")?.Root;
        XElement? text = null;
        if (parent != null)
        {
            text = new XElement("Name", new XAttribute("ID", newId), "New Stage");
            parent.Add(text);
        }
        Items.Add(new UnifiedStage(node, text, parent));
    }

    public override void Delete(UnifiedStage item)
    {
        item.DeleteXml();
        base.Delete(item);
    }
}