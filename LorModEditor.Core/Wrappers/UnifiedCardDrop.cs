using System.Collections.ObjectModel;
using System.Xml.Linq;
using LorModEditor.Core.Attributes;

// 必须引用
// 必须引用

namespace LorModEditor.Core.Wrappers;

public class UnifiedCardDrop : XWrapper
{
    public UnifiedCardDrop(XElement element) : base(element)
    {
        LoadCards();
        InitDefaults();
    }

    // 【修改】改回 string，以便与 BookRepository 和 CardDropRepository 的查找逻辑兼容
    public string BookId
    {
        get => GetAttr(Element, "ID");
        set
        {
            SetAttr(Element, "ID", value);
            OnPropertyChanged(nameof(DisplayName));
        }
    }
    [NoAutoInit]
    public string DisplayName => $"DropTable For Book [{BookId}]";

    // --- 核心修复 ---
    public ObservableCollection<LorId> CardIds { get; } = new();

    private void LoadCards()
    {
        CardIds.Clear();
        foreach (var card in Element.Elements("Card"))
        {
            // 解析时带上当前 Mod 的 PackageId
            CardIds.Add(LorId.ParseXmlReference(card, GlobalId.PackageId));
        }
    }

    public void AddCard(LorId cid)
    {
        if (IsVanilla) return;

        var node = new XElement("Card", cid.ItemId);

        // 如果来源不同，加上 Pid
        if (cid.PackageId != GlobalId.PackageId)
        {
            node.SetAttributeValue("Pid", cid.PackageId);
        }

        Element.Add(node);
        CardIds.Add(cid);
    }

    public void RemoveCard(LorId cid)
    {
        if (IsVanilla) return;

        // 查找匹配的节点 (ID 和 Pid 都要对)
        var node = Element.Elements("Card").FirstOrDefault(x =>
            (x.Attribute("Pid")?.Value ?? GlobalId.PackageId) == cid.PackageId &&
            x.Value == cid.ItemId);

        node?.Remove();
        CardIds.Remove(cid);
    }

    public void DeleteXml()
    {
        if (IsVanilla) return;
        Element.Remove();
    }
}