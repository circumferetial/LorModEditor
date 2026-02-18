using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Attributes;
using Synthesis.Core.Enums;

namespace Synthesis.Feature.Card;

public class UnifiedCard : XWrapper
{
    private readonly XElement _data;

    private readonly XElement? _textParent;

    private XElement? _text;

    public UnifiedCard(XElement data, XElement? text, XElement? textParent)
        : base(data)
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

    public string Id
    {
        get => GetAttr(_data, "ID");
        set
        {
            SetAttr(_data, "ID", value);
            if (_text != null && !IsVanilla)
            {
                SetAttr(_text, "ID", value);
            }
            OnPropertyChanged("DisplayName");
            OnPropertyChanged("GlobalId");
        }
    }

    public string Name
    {
        get => _text?.Element("LocalizedName")?.Value ?? "未翻译";
        set
        {
            if (!IsVanilla)
            {
                EnsureTextNode();
                if (_text != null)
                {
                    SetElementValue(_text, "LocalizedName", value);
                    OnPropertyChanged("DisplayName");
                }
            }
        }
    }

    public ObservableCollection<string> Keywords { get; } = [];

    public Rarity Rarity
    {
        get => GetEnum(_data, "Rarity", Rarity.Common);
        set => SetEnum(_data, "Rarity", value);
    }

    public int Chapter
    {
        get => GetInt(_data, "Chapter");
        set => SetInt(_data, "Chapter", Math.Clamp(value, 0, 7));
    }

    public int Priority
    {
        get => GetInt(_data, "Priority");
        set => SetInt(_data, "Priority", value);
    }

    private XElement SpecNode
    {
        get
        {
            var xElement = _data.Element("Spec");
            if (xElement != null)
            {
                return xElement;
            }
            xElement = new XElement("Spec");
            _data.Add(xElement);
            return xElement;
        }
    }

    public int Cost
    {
        get => GetIntAttr(SpecNode, "Cost");
        set => SetIntAttr(SpecNode, "Cost", value);
    }

    [NoAutoInit]
    public CardRange Range
    {
        get => GetEnumAttr(SpecNode, "Range", CardRange.Near);
        set
        {
            SetEnumAttr(SpecNode, "Range", value);
            switch (value)
            {
                case CardRange.FarArea:
                case CardRange.FarAreaEach:
                    Affection = CardAffection.All;
                    break;
                case CardRange.Far:
                {
                    foreach (var behaviour in Behaviours)
                    {
                        if (behaviour.Detail != DiceDetail.Evasion && behaviour.Detail != DiceDetail.Guard)
                        {
                            behaviour.Motion = DiceMotion.F;
                        }
                    }
                    break;
                }
            }
        }
    }

    public CardAffection Affection
    {
        get => GetEnumAttr(SpecNode, "Affection", CardAffection.One);
        set => SetEnumAttr(SpecNode, "Affection", value);
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

    public ObservableCollection<UnifiedBehaviour> Behaviours { get; } = [];

    public ObservableCollection<string> OptionList { get; } = [];

    private void LoadKeywords()
    {
        Keywords.Clear();
        foreach (var item in _data.Elements("Keyword"))
        {
            Keywords.Add(item.Value);
        }
    }

    public void AddKeyword(string keyword)
    {
        if (!IsVanilla && !Keywords.Contains(keyword))
        {
            _data.Add(new XElement("Keyword", keyword));
            Keywords.Add(keyword);
        }
    }

    public void RemoveKeyword(string keyword)
    {
        if (!IsVanilla)
        {
            _data.Elements("Keyword").FirstOrDefault(x => x.Value == keyword)?.Remove();
            Keywords.Remove(keyword);
        }
    }

    private void LoadBehaviours()
    {
        Behaviours.Clear();
        var xElement = _data.Element("BehaviourList");
        if (xElement == null)
        {
            return;
        }
        foreach (var item in xElement.Elements("Behaviour"))
        {
            Behaviours.Add(new UnifiedBehaviour(item));
        }
    }

    public void AddBehaviour()
    {
        if (!IsVanilla)
        {
            var xElement = _data.Element("BehaviourList");
            if (xElement == null)
            {
                xElement = new XElement("BehaviourList");
                _data.Add(xElement);
            }
            var xElement2 = new XElement("Behaviour", new XAttribute("Min", 1), new XAttribute("Dice", 3),
                new XAttribute("Type", "Atk"), new XAttribute("Detail", "Slash"), new XAttribute("Motion", "J"));
            xElement.Add(xElement2);
            Behaviours.Add(new UnifiedBehaviour(xElement2));
        }
    }

    public void RemoveBehaviour(UnifiedBehaviour b)
    {
        if (!IsVanilla)
        {
            b.Element.Remove();
            Behaviours.Remove(b);
        }
    }

    private void LoadOptions()
    {
        OptionList.Clear();
        foreach (var item in _data.Elements("Option"))
        {
            OptionList.Add(item.Value);
        }
    }

    public void AddOption(string o)
    {
        if (!IsVanilla && !OptionList.Contains(o))
        {
            _data.Add(new XElement("Option", o));
            OptionList.Add(o);
        }
    }

    public void RemoveOption(string o)
    {
        if (!IsVanilla)
        {
            _data.Elements("Option").FirstOrDefault(x => x.Value == o)?.Remove();
            OptionList.Remove(o);
        }
    }

    private void EnsureTextNode()
    {
        if (_text != null || _textParent == null || IsVanilla)
        {
            return;
        }
        var xElement = _textParent;
        if (_text?.Name.LocalName == "BattleCardDescRoot")
        {
            var xElement2 = _textParent.Element("cardDescList");
            if (xElement2 == null)
            {
                xElement2 = new XElement("cardDescList");
                _textParent.Add(xElement2);
            }
            xElement = xElement2;
        }
        _text = new XElement("BattleCardDesc", new XAttribute("ID", Id), new XElement("LocalizedName", "New Card"));
        xElement.Add(_text);
    }

    public void DeleteXml()
    {
        if (!IsVanilla)
        {
            _data.Remove();
            _text?.Remove();
        }
    }
}
