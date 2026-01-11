using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Linq;
using LorModEditor.Core.Attributes;
using LorModEditor.Core.Enums;

namespace LorModEditor.Core.Wrappers;

public class UnifiedCard : XWrapper
{
    private readonly XElement _data;
    private readonly XElement? _textParent;
    private XElement? _text;

    public UnifiedCard(XElement data, XElement? text, XElement? textParent) : base(data)
    {
        _data = data;
        _text = text;
        _textParent = textParent;
        LoadBehaviours();
        LoadOptions();
        LoadKeywords();
        InitDefaults();
    }

    [NoAutoInit] public string DisplayName => $"{GlobalId} {Name}";

    // ID 依然保持 string，因为有些 Modder 喜欢用字符串ID
    public string Id
    {
        get => GetAttr(_data, "ID");
        set
        {
            SetAttr(_data, "ID", value);
            // 同步修改翻译文件的 ID
            if (_text != null && !IsVanilla) SetAttr(_text, "ID", value);

            OnPropertyChanged(nameof(DisplayName));
            OnPropertyChanged(nameof(GlobalId));// GlobalId 也会随之改变
        }
    }

    public string Name
    {
        get => _text?.Element("LocalizedName")?.Value ?? "未翻译";
        set
        {
            if (IsVanilla) return;
            EnsureTextNode();
            if (_text != null)
            {
                SetElementValue(_text, "LocalizedName", value);
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public ObservableCollection<string> Keywords { get; } = [];

    // 强类型属性
    public Rarity Rarity
    {
        get => GetEnum(_data, "Rarity", Rarity.Common);
        set => SetEnum(_data, "Rarity", value);
    }

    public int Chapter
    {
        get => GetInt(_data, "Chapter");
        set => SetInt(_data, "Chapter", Math.Clamp(value, 0, 7));// 限制范围
    }

    public int Priority
    {
        get => GetInt(_data, "Priority");
        set => SetInt(_data, "Priority", value);
    }

    // Spec
    private XElement SpecNode
    {
        get
        {
            var n = _data.Element("Spec");
            if (n != null) return n;
            n = new XElement("Spec");
            _data.Add(n);
            return n;
        }
    }

    public int Cost
    {
        get => GetIntAttr(SpecNode, "Cost");
        set => SetIntAttr(SpecNode, "Cost", value);
    }


    public CardRange Range
    {
        get => GetEnumAttr(SpecNode, "Range", CardRange.Near);
        set
        {
            SetEnumAttr(SpecNode, "Range", value);
            if (value != CardRange.Far) return;
            foreach (var b in Behaviours)
            {
                if (b.Detail != DiceDetail.Evasion && b.Detail != DiceDetail.Guard)
                    b.Motion = DiceMotion.F;
            }
        }
    }

    public string Script
    {
        get => GetElementValue(_data, "Script");
        set => SetElementValue(_data, "Script", value);
    }

    public string ArtworkName
    {
        get => GetElementValue(_data, "Artwork");
        set => SetElementValue(_data, "Artwork", Path.GetFileNameWithoutExtension(value));
    }

    public ObservableCollection<UnifiedBehaviour> Behaviours { get; } = new();

    public ObservableCollection<string> OptionList { get; } = new();

    private void LoadKeywords()
    {
        Keywords.Clear();
        foreach (var k in _data.Elements("Keyword"))
        {
            Keywords.Add(k.Value);
        }
    }

    public void AddKeyword(string keyword)
    {
        if (IsVanilla) return;
        if (Keywords.Contains(keyword)) return;

        _data.Add(new XElement("Keyword", keyword));
        Keywords.Add(keyword);
    }

    public void RemoveKeyword(string keyword)
    {
        if (IsVanilla) return;

        var node = _data.Elements("Keyword").FirstOrDefault(x => x.Value == keyword);
        node?.Remove();
        Keywords.Remove(keyword);
    }

    private void LoadBehaviours()
    {
        Behaviours.Clear();
        var list = _data.Element("BehaviourList");
        if (list != null)
            foreach (var b in list.Elements("Behaviour"))
                Behaviours.Add(new UnifiedBehaviour(b));
    }

    public void AddBehaviour()
    {
        if (IsVanilla) return;
        var list = _data.Element("BehaviourList");
        if (list == null)
        {
            list = new XElement("BehaviourList");
            _data.Add(list);
        }
        var newDice = new XElement("Behaviour", new XAttribute("Min", 1), new XAttribute("Dice", 3),
            new XAttribute("Type", "Atk"), new XAttribute("Detail", "Slash"), new XAttribute("Motion", "J"));
        list.Add(newDice);
        Behaviours.Add(new UnifiedBehaviour(newDice));
    }

    public void RemoveBehaviour(UnifiedBehaviour b)
    {
        if (IsVanilla) return;
        b.Element.Remove();
        Behaviours.Remove(b);
    }

    private void LoadOptions()
    {
        OptionList.Clear();
        foreach (var o in _data.Elements("Option")) OptionList.Add(o.Value);
    }

    public void AddOption(string o)
    {
        if (IsVanilla || OptionList.Contains(o)) return;
        _data.Add(new XElement("Option", o));
        OptionList.Add(o);
    }

    public void RemoveOption(string o)
    {
        if (IsVanilla) return;
        _data.Elements("Option").FirstOrDefault(x => x.Value == o)?.Remove();
        OptionList.Remove(o);
    }

// Wrappers/UnifiedCard.cs

    private void EnsureTextNode()
    {
        if (_text != null || _textParent == null || IsVanilla) return;

        // 【核心修复】检查父节点是否是 cardDescList
        // 如果 _textParent 是 Root (BattleCardDescRoot)，我们需要先找/创 cardDescList
        var realParent = _textParent;

        if (_text?.Name.LocalName == "BattleCardDescRoot")
        {
            var listNode = _textParent.Element("cardDescList");
            if (listNode == null)
            {
                listNode = new XElement("cardDescList");
                _textParent.Add(listNode);
            }
            realParent = listNode;
        }

        _text = new XElement("BattleCardDesc", new XAttribute("ID", Id), new XElement("LocalizedName", "New Card"));

        // 添加到正确的父节点
        realParent.Add(_text);
    }

    public void DeleteXml()
    {
        if (IsVanilla) return;
        _data.Remove();
        _text?.Remove();
    }
}