using System.IO;
using System.Xml.Linq;
using Synthesis.Core;
using Synthesis.Core.Abstraction;

namespace Synthesis.Feature.Dialog;

public class DialogRepository : BaseRepository<UnifiedCharacterDialog>
{
    private const string DialogFolderName = "BattleDialogues";

    public override void LoadResources(string root, string lang, string modId)
    {
        LoadDialogResources(root, lang, modId);
    }

    public override void LoadLocResources(string projectRoot, string language, string modId)
    {
        LoadDialogResources(projectRoot, language, modId);
    }

    private void LoadDialogResources(string root, string lang, string modId)
    {
        ScanAndLoad(Path.Combine(root, "Localize", lang, "BattleDialogues"), "BattleDialogRoot", modId, AddDataDoc);
    }

    public override void ClearLocOnly()
    {
        _modDataDocs.Clear();
        _vanillaDataDocs.Clear();
        NotifyStatusChanged();
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        if (!HasModData)
        {
            CreateXmlTemplate(Path.Combine(root, "Localize", lang, "BattleDialogues", "BattleDialogues.xml"),
                "BattleDialogRoot", modId, AddDataDoc);
        }
    }

    public override void Parse(bool containOriginal)
    {
        Items.Clear();
        IEnumerable<XDocument> enumerable;
        if (!containOriginal)
        {
            IEnumerable<XDocument> modDataDocs = _modDataDocs;
            enumerable = modDataDocs;
        }
        else
        {
            enumerable = _dataDocs;
        }
        foreach (var item in enumerable)
        {
            if (item.Root?.Name.LocalName != "BattleDialogRoot")
            {
                continue;
            }
            foreach (var item2 in item.Root.Elements("Character"))
            {
                Items.Add(new UnifiedCharacterDialog(item2));
            }
        }
    }

    public void CreateTemplate(string bookId)
    {
        if (string.IsNullOrEmpty(bookId))
        {
            return;
        }
        if (Items.Any(x => x.CharacterId == bookId))
        {
            throw new Exception("ID 为 " + bookId + " 的角色对话已存在！");
        }
        var targetDataDoc = GetTargetDataDoc("BattleDialogRoot");
        if (targetDataDoc == null)
        {
            throw new Exception("未找到可写入的 BattleDialogs.xml 文件。");
        }
        var xElement = new XElement("Character", new XAttribute("ID", bookId));
        foreach (var dialogType in GlobalValues.DialogTypes)
        {
            var xElement2 = new XElement("Type", new XAttribute("ID", dialogType));
            for (var num = 0; num < 3; num++)
            {
                var value = $"{dialogType}_{num}";
                xElement2.Add(new XElement("BattleDialog", new XAttribute("ID", value), "在此填入台词..."));
            }
            xElement.Add(xElement2);
        }
        targetDataDoc.Root?.Add(xElement);
        Items.Add(new UnifiedCharacterDialog(xElement));
    }
}
