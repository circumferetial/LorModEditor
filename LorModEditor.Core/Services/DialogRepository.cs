using System.IO;
using System.Xml.Linq;
using LorModEditor.Core.Wrappers;

namespace LorModEditor.Core.Services;

public class DialogRepository : BaseRepository<UnifiedCharacterDialog>
{
    // 扫描路径
    public override void LoadResources(string root, string lang, string modId)
    {
        // 扫描 StaticInfo 下的对话文件
        ScanAndLoad(Path.Combine(root, "Localize", lang, "BattleDialogues"), "BattleDialogRoot", modId, AddDataDoc);
    }

    // 确保文件存在
    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasData)
        {
            CreateXmlTemplate(Path.Combine(root, "Localize", lang, "BattleDialogues"), "BattleDialogRoot", modId,
                AddDataDoc);
        }
    }

    // 加载逻辑
    public override void Load()
    {
        foreach (var doc in _dataDocs)
        {
            if (doc.Root?.Name.LocalName != "BattleDialogRoot") continue;
            foreach (var node in doc.Root.Elements("Character"))
            {
                Items.Add(new UnifiedCharacterDialog(node));
            }
        }
    }

    // 【核心】根据 BookID 生成模板
    public void CreateTemplate(string bookId)
    {
        if (string.IsNullOrEmpty(bookId)) return;

        // 1. 检查是否已存在
        if (Items.Any(x => x.CharacterId == bookId))
        {
            throw new Exception($"ID 为 {bookId} 的角色对话已存在！");
        }

        // 2. 找文件写入
        var targetDoc = GetTargetDataDoc("BattleDialogRoot");
        if (targetDoc == null) throw new Exception("未找到可写入的 Combat_Dialog.xml 文件。");

        // 3. 构建 XML 结构 (这就是你要的“默认实现版本”)
        var charNode = new XElement("Character", new XAttribute("ID", bookId));

        foreach (var type in GlobalValues.DialogTypes)
        {
            var typeNode = new XElement("Type", new XAttribute("ID", type));

            // 默认生成 3 句空台词，方便用户填
            // 格式：START_BATTLE_0, START_BATTLE_1 ...
            for (var i = 0; i < 3; i++)
            {
                var logId = $"{type}_{i}";
                typeNode.Add(new XElement("BattleDialog", new XAttribute("ID", logId), "在此填入台词..."));
            }
            charNode.Add(typeNode);
        }

        // 4. 写入并保存
        targetDoc.Root?.Add(charNode);
        Items.Add(new UnifiedCharacterDialog(charNode));
    }
}