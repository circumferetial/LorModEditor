using System.Collections.ObjectModel;
using System.Xml.Linq;
using Synthesis.Core;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Attributes;
using Synthesis.Core.Enums;

namespace Synthesis.Feature.Book;

public class UnifiedBook : XWrapper
{
    private static readonly char[] separator = ['\r', '\n'];

    private readonly XElement _data;

    private readonly XElement? _textParent;

    private XElement? _text;

    public UnifiedBook(XElement data, XElement? text, XElement? textParent)
        : base(data)
    {
        _data = data;
        _text = text;
        _textParent = textParent;
        LoadPassives();
        LoadOnlyCards();
        InitDefaults();
    }

    public string SkinType
    {
        get => GetElementValue(_data, "CharacterSkinType", "Lor");
        set => SetElementValue(_data, "CharacterSkinType", value);
    }

    private XElement EffectNode
    {
        get
        {
            var xElement = _data.Element("EquipEffect");
            if (xElement == null)
            {
                xElement = new XElement("EquipEffect");
                _data.Add(xElement);
            }
            return xElement;
        }
    }

    public string BookIcon
    {
        get => GetElementValue(_data, "BookIcon", "FullStopOffice");
        set => SetElementValue(_data, "BookIcon", value);
    }

    public string CharacterSkin
    {
        get => GetElementValue(_data, "CharacterSkin", "Liwei");
        set => SetElementValue(_data, "CharacterSkin", value);
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
                SetAttr(_text, "BookID", value);
            }
            OnPropertyChanged("DisplayName");
            OnPropertyChanged("GlobalId");
        }
    }

    public LorId IdStruct => GlobalId;

    public string Name
    {
        get => _text?.Element("BookName")?.Value ?? "未翻译";
        set
        {
            if (!IsVanilla)
            {
                EnsureTextNode();
                if (_text != null)
                {
                    SetElementValue(_text, "BookName", value);
                }
                OnPropertyChanged("DisplayName");
            }
        }
    }

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

    public int Chapter
    {
        get => GetInt(_data, "Chapter", 1);
        set => SetInt(_data, "Chapter", Math.Clamp(value, 1, 7));
    }

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
    }

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

    public string BookStory
    {
        get
        {
            var xElement = _text?.Element("TextList");
            if (xElement == null)
            {
                return "";
            }
            IEnumerable<string> values = from e in xElement.Elements("Desc")
                select e.Value;
            return string.Join(Environment.NewLine, values);
        }
        set
        {
            if (IsVanilla)
            {
                return;
            }
            EnsureTextNode();
            if (_text != null)
            {
                var xElement = _text.Element("TextList");
                if (xElement == null)
                {
                    xElement = new XElement("TextList");
                    _text.Add(xElement);
                }
                xElement.RemoveAll();
                if (!string.IsNullOrEmpty(value))
                {
                    var array = value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var content in array)
                    {
                        xElement.Add(new XElement("Desc", content));
                    }
                }
            }
            OnPropertyChanged();
        }
    }

    public ObservableCollection<LorId> Passives { get; } = [];

    public ObservableCollection<LorId> OnlyCards { get; } = [];

    private void LoadPassives()
    {
        Passives.Clear();
        if (_data.Element("EquipEffect") == null)
        {
            return;
        }
        foreach (var item in EffectNode.Elements("Passive"))
        {
            Passives.Add(LorId.ParseXmlReference(item, GlobalId.PackageId));
        }
    }

    private void LoadOnlyCards()
    {
        OnlyCards.Clear();
        if (_data.Element("EquipEffect") == null)
        {
            return;
        }
        foreach (var item in EffectNode.Elements("OnlyCard"))
        {
            OnlyCards.Add(LorId.ParseXmlReference(item, GlobalId.PackageId));
        }
    }

    public void AddPassive(LorId pid)
    {
        if (!IsVanilla)
        {
            var xElement = new XElement("Passive", pid.ItemId);
            if (pid.PackageId != GlobalId.PackageId)
            {
                xElement.SetAttributeValue("Pid", pid.PackageId);
            }
            EffectNode.Add(xElement);
            Passives.Add(pid);
        }
    }

    public void RemovePassive(LorId pid)
    {
        if (!IsVanilla)
        {
            EffectNode.Elements("Passive").FirstOrDefault(x =>
                (x.Attribute("Pid")?.Value ?? GlobalId.PackageId) == pid.PackageId && x.Value == pid.ItemId)?.Remove();
            Passives.Remove(pid);
        }
    }

    public void AddOnlyCard(LorId cid)
    {
        if (!IsVanilla)
        {
            var xElement = new XElement("OnlyCard", cid.ItemId);
            if (cid.PackageId != GlobalId.PackageId)
            {
                xElement.SetAttributeValue("Pid", cid.PackageId);
            }
            EffectNode.Add(xElement);
            OnlyCards.Add(cid);
        }
    }

    public void RemoveOnlyCard(LorId cid)
    {
        if (!IsVanilla)
        {
            EffectNode.Elements("OnlyCard").FirstOrDefault(x =>
                (x.Attribute("Pid")?.Value ?? GlobalId.PackageId) == cid.PackageId && x.Value == cid.ItemId)?.Remove();
            OnlyCards.Remove(cid);
        }
    }

    private void EnsureTextNode()
    {
        if (_text != null || _textParent == null || IsVanilla)
        {
            return;
        }
        var xElement = _textParent;
        if (_textParent.Name.LocalName == "BookDescRoot")
        {
            var xElement2 = _textParent.Element("bookDescList");
            if (xElement2 == null)
            {
                xElement2 = new XElement("bookDescList");
                _textParent.Add(xElement2);
            }
            xElement = xElement2;
        }
        _text = new XElement("BookDesc", new XAttribute("BookID", Id));
        _text.Add(new XElement("BookName", "New Book"));
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
