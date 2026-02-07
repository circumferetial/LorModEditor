using System.IO;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Extensions;

namespace Synthesis.Feature.Card;

public class CardRepository : BaseRepository<UnifiedCard>
{
    public override void LoadResources(string root, string lang, string modId)
    {
        ScanAndLoad(Path.Combine(root, @"StaticInfo\Card"), "DiceCardXmlRoot", modId, AddDataDoc);
        ScanAndLoad(Path.Combine(root, $@"Localize\{lang}\BattlesCards"), "BattleCardDescRoot", modId, AddLocDoc);
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasData)
            CreateXmlTemplate(Path.Combine(root, @"StaticInfo\Card\CardInfo.xml"), "DiceCardXmlRoot", modId,
                AddDataDoc);
        if (!HasLoc)
            CreateXmlTemplate(Path.Combine(root, $@"Localize\{lang}\BattlesCards\BattlesCards.xml"),
                "BattleCardDescRoot", modId, AddLocDoc);
    }

    public void DuplicateCard(UnifiedCard? sourceCard)
    {
        if (sourceCard == null) return;

        // 1. 确定目标数据文件 (必须是非原版)
        // 如果源卡是原版卡，我们就把它复制到当前的 Mod 文件里
        // 如果源卡是 Mod 卡，就复制到它所在的文件里
        var targetDataDoc = sourceCard.IsVanilla ? GetTargetDataDoc("DiceCardXmlRoot") : sourceCard.Element.Document;

        if (targetDataDoc?.Root == null)
        {
            throw new Exception("未找到可写入的 CardInfo 文件");
        }

        // 2. 计算新 ID
        var newId = 10000000;
        if (Items.Any(x => !x.IsVanilla))
            newId = Items.Where(x => !x.IsVanilla).Max(x => int.TryParse(x.Id, out var i) ? i : 0) + 1;
        var strId = newId.ToString();

        // 3. 克隆数据节点 (Deep Copy)
        var newData = new XElement(sourceCard.Element);
        // 修改 ID
        newData.SetAttributeValue("ID", strId);

        // 写入数据文件
        targetDataDoc.Root.Add(newData);

        // 4. 克隆翻译节点 (如果有)
        // 我们需要手动去翻译文件里找，或者根据 sourceCard 的 Name 构造一个新的
        var parentLoc = GetTargetLocDoc("BattleCardDescRoot")?.Root?.Element("cardDescList");
        if (parentLoc == null)
        {
            var listNode = new XElement("cardDescList");
            GetTargetLocDoc("BattleCardDescRoot")?.Root?.Add(listNode);

            parentLoc = listNode;
        }

        var newText = new XElement("BattleCardDesc", new XAttribute("ID", strId));
        // 复制原名 + (Copy)
        newText.Add(new XElement("LocalizedName", sourceCard.Name + " (Copy)"));


        parentLoc.Add(newText);

        // 5. 添加到 UI
        // 注意：如果是复制原版卡，新卡是 Mod 卡，所以 Parent 要传 modLocParent
        var newWrapper = new UnifiedCard(newData, newText, parentLoc);
        Items.Add(newWrapper);
    }

    public override void Load()
    {
        var modLocParent = GetTargetLocDoc("BattleCardDescRoot")?.Root?.Element("cardDescList") ??
                           GetTargetLocDoc("BattleCardDescRoot")?.Root;
        foreach (var doc in _dataDocs)
        {
            if (doc.Root?.Name.LocalName != "DiceCardXmlRoot") continue;
            foreach (var node in doc.Root.Elements("Card"))
            {
                var id = node.Attribute("ID")?.Value ?? "";
                if (string.IsNullOrEmpty(id)) continue;

                XElement? foundText = null;
                var isVanilla = doc.IsVanilla();

                var targetLocs = _locDocs.Where(d => d.Root?.Name.LocalName == "BattleCardDescRoot").ToArray();
                foreach (var loc in targetLocs)
                {
                    if (loc.IsVanilla() == isVanilla)
                    {
                        foundText = loc.Descendants("BattleCardDesc")
                            .FirstOrDefault(x => x.Attribute("ID")?.Value == id);
                        if (foundText != null) break;
                    }
                }
                if (foundText == null)
                {
                    foreach (var loc in targetLocs)
                    {
                        if (loc.IsVanilla() != isVanilla)
                        {
                            foundText = loc.Descendants("BattleCardDesc")
                                .FirstOrDefault(x => x.Attribute("ID")?.Value == id);
                            if (foundText != null) break;
                        }
                    }
                }
                Items.Add(new UnifiedCard(node, foundText, modLocParent));
            }
        }
    }

    public UnifiedCard Create()
    {
        var targetDoc = GetTargetDataDoc("DiceCardXmlRoot");
        if (targetDoc == null) throw new Exception("未找到可写入的 CardInfo 文件(非原版)");

        var newId = 10000000;
        if (Items.Any(x => !x.IsVanilla))
            newId = Items.Where(x => !x.IsVanilla).Max(x => int.TryParse(x.Id, out var i) ? i : 0) + 1;
        var strId = newId.ToString();

        // 1. 创建数据节点
        var node = new XElement("Card", new XAttribute("ID", strId));
        node.Add(new XElement("Rarity", "Common"));
        node.Add(new XElement("Spec", new XAttribute("Cost", 0), new XAttribute("Range", "Near")));
        node.Add(new XElement("Script", ""));
        targetDoc.Root?.Add(node);

        // 2. 创建翻译节点
        var locDoc = GetTargetLocDoc("BattleCardDescRoot");
        var parent = locDoc?.Root;

        if (parent != null)
        {
            // 【核心修复】检查是否存在 cardDescList 层级
            var listNode = parent.Element("cardDescList");
            if (listNode == null)
            {
                // 如果不存在，创建一个，并挂载到 Root 下
                listNode = new XElement("cardDescList");
                parent.Add(listNode);
            }

            // 将 parent 更新为 listNode，这样新节点就会加在 <cardDescList> 里面
            parent = listNode;

            // 创建翻译内容
            var text = new XElement("BattleCardDesc", new XAttribute("ID", strId));
            text.Add(new XElement("LocalizedName", "New Card"));
            parent.Add(text);

            // 3. 添加到 UI
            var unifiedCard = new UnifiedCard(node, text, parent);
            Items.Add(unifiedCard);
            return unifiedCard;
        }
        else
        {
            // 如果没找到翻译文件，只加数据
            var unifiedCard = new UnifiedCard(node, null, null);
            Items.Add(unifiedCard);
            return unifiedCard;
        }
    }

    public override void Delete(UnifiedCard item)
    {
        item.DeleteXml();
        base.Delete(item);
    }
}