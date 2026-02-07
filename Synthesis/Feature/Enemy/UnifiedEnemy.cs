using System.Collections.ObjectModel;
using System.Xml.Linq;
using Synthesis.Core;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Attributes;

// 必须引用
// 必须引用

namespace Synthesis.Feature.Enemy;

// --- 子包装器：掉落项 ---
public class UnifiedEnemyDrop : XWrapper
{
    public UnifiedEnemyDrop(XElement element) : base(element)
    {
        InitDefaults();
    }

    // 掉落概率 (通常是小数或整数权重，这里用 double 比较通用)
    // 比如 0.5 或 50
    public string Prob
    {
        // 建议保持 string 以支持 "0.5" 这种写法，或者用 double
        get => GetAttr(Element, "Prob", "1");
        set => SetAttr(Element, "Prob", value);
    }

    // 掉落的书籍 ID (可能是原版书，所以用 LorId)
    public LorId BookId
    {
        get => LorId.ParseXmlReference(Element, GlobalId.PackageId);
        set
        {
            if (IsVanilla) return;
            Element.Value = value.ItemId;
            if (value.PackageId != GlobalId.PackageId) Element.SetAttributeValue("Pid", value.PackageId);
            else Element.Attribute("Pid")?.Remove();
            OnPropertyChanged();
        }
    }
}

// --- 主包装器：敌人 ---
public class UnifiedEnemy : XWrapper
{
    private readonly XElement? _deckParent;

    private readonly XElement? _textParent;
    private readonly XElement _unitData;
    private XElement? _deckData;
    private XElement? _text;

    public UnifiedEnemy(XElement unit, XElement? deck, XElement? text, XElement? tp, XElement? dp) : base(unit)
    {
        _unitData = unit;
        _deckData = deck;
        _text = text;
        _textParent = tp;
        _deckParent = dp;
        LoadDeckCards();
        LoadDrops();
        InitDefaults();
    }

    [NoAutoInit] public string DisplayName => $"{GlobalId} {Name}";

    // --- ID (改为 string 以保持统一) ---
    public string Id
    {
        get => GetAttr(_unitData, "ID");
        set
        {
            SetAttr(_unitData, "ID", value);
            // 保持 Deck ID 一致
            if (_deckData != null && !IsVanilla) SetAttr(_deckData, "ID", value);

            // NameID 默认跟随 ID
            SetElementValue(_unitData, "NameID", value);

            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(GlobalId));
        }
    }

    // NameID 也用 string
    public string NameId
    {
        get => GetElementValue(_unitData, "NameID", Id);
        set => SetElementValue(_unitData, "NameID", value);
    }

    public string Name
    {
        get => _text?.Value ?? "未翻译";
        set
        {
            if (IsVanilla) return;
            EnsureTextNode();
            _text?.Value = value;
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    // --- 强类型属性 (int/bool) ---
    public int MinHeight
    {
        get => GetInt(_unitData, "MinHeight", 175);
        set => SetInt(_unitData, "MinHeight", value);
    }

    public int MaxHeight
    {
        get => GetInt(_unitData, "MaxHeight", 185);
        set => SetInt(_unitData, "MaxHeight", value);
    }

    public bool Retreat
    {
        get => GetBool(_unitData, "Retreat");
        set => SetBool(_unitData, "Retreat", value);
    }

    // 核心书页引用 (LorId)
    public LorId BookId
    {
        get => GetLorId(_unitData, "BookId");
        set => SetLorId(_unitData, "BookId", value);
    }

    // --- 掉落表 (DropTable) ---
    private XElement DropTableNode
    {
        get
        {
            var n = _unitData.Element("DropTable");
            if (n == null)
            {
                n = new XElement("DropTable", new XAttribute("Level", "0"));
                _unitData.Add(n);
            }
            return n;
        }
    }

    public int DropLevel
    {
        get => GetInt(DropTableNode, "Level");
        set => SetInt(DropTableNode, "Level", value);
    }

    // 掉落集合
    public ObservableCollection<UnifiedEnemyDrop> Drops { get; } = new();

    // --- 卡组 (Deck) - 改为 LorId 列表 ---
    public ObservableCollection<LorId> DeckCardIds { get; } = new();

    private void LoadDrops()
    {
        Drops.Clear();
        if (_unitData.Element("DropTable") == null) return;
        foreach (var i in DropTableNode.Elements("DropItem"))
            Drops.Add(new UnifiedEnemyDrop(i));
    }

    public void AddDrop(LorId bookId)
    {
        if (IsVanilla) return;

        // 默认概率 1
        var n = new XElement("DropItem", new XAttribute("Prob", "1"), bookId.ItemId);
        if (bookId.PackageId != GlobalId.PackageId) n.SetAttributeValue("Pid", bookId.PackageId);

        DropTableNode.Add(n);
        Drops.Add(new UnifiedEnemyDrop(n));
    }

    public void RemoveDrop(UnifiedEnemyDrop i)
    {
        if (IsVanilla) return;
        i.Element.Remove();
        Drops.Remove(i);
    }

    private void LoadDeckCards()
    {
        DeckCardIds.Clear();
        if (_deckData != null)
        {
            foreach (var c in _deckData.Elements("Card"))
            {
                // 解析 Pid
                DeckCardIds.Add(LorId.ParseXmlReference(c, GlobalId.PackageId));
            }
        }
    }

    public void AddCardToDeck(LorId cid)
    {
        if (IsVanilla) return;
        EnsureDeckNode();

        if (_deckData != null)
        {
            var node = new XElement("Card", cid.ItemId);
            // 处理跨 Mod 引用
            if (cid.PackageId != GlobalId.PackageId) node.SetAttributeValue("Pid", cid.PackageId);

            _deckData.Add(node);
            DeckCardIds.Add(cid);
        }
    }

    public void RemoveCardFromDeck(LorId cid)
    {
        if (IsVanilla || _deckData == null) return;

        // 查找节点
        var node = _deckData.Elements("Card").FirstOrDefault(x =>
        {
            // 获取 XML 里的 Pid，如果没有则视为当前 Mod 的 Pid
            var nodePid = x.Attribute("Pid")?.Value ?? GlobalId.PackageId;
            var nodeId = x.Value;

            // 比较 (忽略大小写更稳妥)
            return string.Equals(nodePid, cid.PackageId, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(nodeId, cid.ItemId, StringComparison.OrdinalIgnoreCase);
        });

        node?.Remove();
        DeckCardIds.Remove(cid);
    }

    // --- 辅助 ---
    private void EnsureTextNode()
    {
        if (_text != null || _textParent == null || IsVanilla) return;
        _text = new XElement("Name", new XAttribute("ID", NameId), "New Enemy");
        _textParent.Add(_text);
    }

    private void EnsureDeckNode()
    {
        if (_deckData != null || _deckParent == null || IsVanilla) return;
        _deckData = new XElement("Deck", new XAttribute("ID", Id));
        _deckParent.Add(_deckData);
    }

    public void DeleteXml()
    {
        if (IsVanilla) return;
        _unitData.Remove();
        _deckData?.Remove();
        _text?.Remove();
    }
}