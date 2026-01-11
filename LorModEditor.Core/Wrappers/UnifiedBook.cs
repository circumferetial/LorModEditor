using System.Collections.ObjectModel;
using System.Xml.Linq;
using LorModEditor.Core.Attributes;
using LorModEditor.Core.Enums;

// 确保引用了 Enums

namespace LorModEditor.Core.Wrappers;

public class UnifiedBook : XWrapper
{
    private static readonly char[] separator = ['\r', '\n'];
    private readonly XElement _data;
    private readonly XElement? _textParent;
    private XElement? _text;

    public UnifiedBook(XElement data, XElement? text, XElement? textParent) : base(data)
    {
        _data = data;
        _text = text;
        _textParent = textParent;
        LoadPassives();
        LoadOnlyCards();
        InitDefaults();
    }

    // 懒加载 EquipEffect 节点
    private XElement EffectNode
    {
        get
        {
            var node = _data.Element("EquipEffect");
            if (node == null)
            {
                node = new XElement("EquipEffect");
                _data.Add(node);
            }
            return node;
        }
    }

    public string BookIcon
    {
        get => GetElementValue(_data, "BookIcon", "FullStopOffice");
        set => SetElementValue(_data, "BookIcon", value);
    }

    // 【新增】CharacterSkin (默认值: Liwei 或 Default)
    // 默认皮肤通常是 "Default" (穿核心书页外观)，或者你可以指定一个默认值
    public string CharacterSkin
    {
        get => GetElementValue(_data, "CharacterSkin", "Liwei");
        set => SetElementValue(_data, "CharacterSkin", value);
    }

    [NoAutoInit] public string DisplayName => $"{GlobalId} {Name}";

    // --- ID (LorId) ---
    // 注意：这里的 ID 实际上是指向自身的 GlobalId，而不是引用别人的 ID
    // 虽然用 LorId 没问题，但要注意 BookDesc 里的同步逻辑
    public string Id
    {
        get => GetAttr(_data, "ID");
        set
        {
            SetAttr(_data, "ID", value);
            // 同步修改翻译ID (BookDesc 用的是 BookID 属性)
            if (_text != null && !IsVanilla)
            {
                SetAttr(_text, "BookID", value);// 只需要存数字ID
            }
            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(GlobalId));
        }
    }

    // 快捷获取强类型 LorId (用于绑定 ComboBox SelectedValue)
    public LorId IdStruct => GlobalId;

    public string Name
    {
        get => _text?.Element("BookName")?.Value ?? "未翻译";
        set
        {
            if (IsVanilla) return;
            EnsureTextNode();
            if (_text != null) SetElementValue(_text, "BookName", value);
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    // --- 基础属性 --- 
    public int Episode
    {
        get => GetInt(_data, "Episode");
        set => SetInt(_data, "Episode", value);
    }

    public Rarity Rarity
    {
        get => GetEnum(_data, "Rarity", Rarity.Common);
        set => SetEnum(_data, "Rarity", value);
    }

    public RangeType RangeType
    {
        get => GetEnum(_data, "RangeType", RangeType.Hybrid);
        set => SetEnum(_data, "RangeType", value);
    }

    // Chapter 限制 1-7
    public int Chapter
    {
        get => GetInt(_data, "Chapter", 1);
        set => SetInt(_data, "Chapter", Math.Clamp(value, 1, 7));
    }

    // --- 战斗数值 (强类型 int) ---
    public int HP
    {
        get => GetInt(EffectNode, "HP", 10);
        set => SetInt(EffectNode, "HP", value);
    }

    public int Break
    {
        get => GetInt(EffectNode, "Break", 10);
        set => SetInt(EffectNode, "Break", value);
    }

    public int SpeedMin
    {
        get => GetInt(EffectNode, "SpeedMin", 1);
        set => SetInt(EffectNode, "SpeedMin", value);
    }

    public int Speed
    {
        get => GetInt(EffectNode, "Speed", 1);
        set => SetInt(EffectNode, "Speed", value);
    }// MaxSpeed

    public int StartPlayPoint
    {
        get => GetInt(EffectNode, "StartPlayPoint", 3);
        set => SetInt(EffectNode, "StartPlayPoint", value);
    }

    public int MaxPlayPoint
    {
        get => GetInt(EffectNode, "MaxPlayPoint", 3);
        set => SetInt(EffectNode, "MaxPlayPoint", value);
    }

    // --- 抗性 (强类型 Enum) ---
    public AtkResist SResist
    {
        get => GetEnum(EffectNode, "SResist", AtkResist.Normal);
        set => SetEnum(EffectNode, "SResist", value);
    }

    public AtkResist PResist
    {
        get => GetEnum(EffectNode, "PResist", AtkResist.Normal);
        set => SetEnum(EffectNode, "PResist", value);
    }

    public AtkResist HResist
    {
        get => GetEnum(EffectNode, "HResist", AtkResist.Normal);
        set => SetEnum(EffectNode, "HResist", value);
    }

    public AtkResist SBResist
    {
        get => GetEnum(EffectNode, "SBResist", AtkResist.Normal);
        set => SetEnum(EffectNode, "SBResist", value);
    }

    public AtkResist PBResist
    {
        get => GetEnum(EffectNode, "PBResist", AtkResist.Normal);
        set => SetEnum(EffectNode, "PBResist", value);
    }

    public AtkResist HBResist
    {
        get => GetEnum(EffectNode, "HBResist", AtkResist.Normal);
        set => SetEnum(EffectNode, "HBResist", value);
    }

    // --- 书籍故事 ---
    public string BookStory
    {
        get
        {
            var textList = _text?.Element("TextList");
            if (textList == null) return "";
            var paragraphs = textList.Elements("Desc").Select(e => e.Value);
            return string.Join(Environment.NewLine, paragraphs);
        }
        set
        {
            if (IsVanilla) return;
            EnsureTextNode();
            if (_text != null)
            {
                var textList = _text.Element("TextList");
                if (textList == null)
                {
                    textList = new XElement("TextList");
                    _text.Add(textList);
                }

                textList.RemoveAll();
                if (!string.IsNullOrEmpty(value))
                {
                    var lines = value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines) textList.Add(new XElement("Desc", line));
                }
            }
            OnPropertyChanged();
        }
    }

    // --- 集合 ---
    public ObservableCollection<LorId> Passives { get; } = new();
    public ObservableCollection<LorId> OnlyCards { get; } = new();

    private void LoadPassives()
    {
        Passives.Clear();
        if (_data.Element("EquipEffect") == null) return;
        foreach (var p in EffectNode.Elements("Passive"))
            Passives.Add(LorId.ParseXmlReference(p, GlobalId.PackageId));
    }

    private void LoadOnlyCards()
    {
        OnlyCards.Clear();
        if (_data.Element("EquipEffect") == null) return;
        foreach (var c in EffectNode.Elements("OnlyCard"))
            OnlyCards.Add(LorId.ParseXmlReference(c, GlobalId.PackageId));
    }

    public void AddPassive(LorId pid)
    {
        if (IsVanilla) return;
        var node = new XElement("Passive", pid.ItemId);
        if (pid.PackageId != GlobalId.PackageId) node.SetAttributeValue("Pid", pid.PackageId);
        EffectNode.Add(node);
        Passives.Add(pid);
    }

    public void RemovePassive(LorId pid)
    {
        if (IsVanilla) return;
        var node = EffectNode.Elements("Passive").FirstOrDefault(x =>
            (x.Attribute("Pid")?.Value ?? GlobalId.PackageId) == pid.PackageId && x.Value == pid.ItemId);
        node?.Remove();
        Passives.Remove(pid);
    }

    public void AddOnlyCard(LorId cid)
    {
        if (IsVanilla) return;
        var node = new XElement("OnlyCard", cid.ItemId);
        if (cid.PackageId != GlobalId.PackageId) node.SetAttributeValue("Pid", cid.PackageId);
        EffectNode.Add(node);
        OnlyCards.Add(cid);
    }

    public void RemoveOnlyCard(LorId cid)
    {
        if (IsVanilla) return;
        var node = EffectNode.Elements("OnlyCard").FirstOrDefault(x =>
            (x.Attribute("Pid")?.Value ?? GlobalId.PackageId) == cid.PackageId && x.Value == cid.ItemId);
        node?.Remove();
        OnlyCards.Remove(cid);
    }

// Wrappers/UnifiedBook.cs

    private void EnsureTextNode()
    {
        if (_text != null || _textParent == null || IsVanilla) return;

        var realParent = _textParent;

        // 【核心修复】如果父节点是 Root，且没到 bookDescList 这一层，就补上
        if (_textParent.Name.LocalName == "BookDescRoot")
        {
            var listNode = _textParent.Element("bookDescList");
            if (listNode == null)
            {
                listNode = new XElement("bookDescList");
                _textParent.Add(listNode);
            }
            realParent = listNode;
        }

        _text = new XElement("BookDesc", new XAttribute("BookID", Id));
        _text.Add(new XElement("BookName", "New Book"));

        realParent.Add(_text);
    }

    public void DeleteXml()
    {
        if (IsVanilla) return;
        _data.Remove();
        _text?.Remove();
    }
}