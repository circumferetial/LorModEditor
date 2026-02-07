using System.Xml.Linq;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Attributes;
using Synthesis.Core.Enums;

namespace Synthesis.Feature.Passive;

public class UnifiedPassive : XWrapper
{
    private readonly XElement _data;
    private readonly XElement? _textParent;
    private XElement? _text;

    public UnifiedPassive(XElement data, XElement? text, XElement? textParent) : base(data)
    {
        _textParent = textParent;
        _data = data;
        _text = text;
        InitDefaults();
    }

    [NoAutoInit] public string DisplayName => $"{GlobalId} {Name}";

    public string Id
    {
        get => GetAttr(_data, "ID");
        set
        {
            SetAttr(_data, "ID", value);
            if (_text != null && !IsVanilla) SetAttr(_text, "ID", value);
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    public int InnerTypeId
    {
        get => GetInt(_data, "InnerType", -1);
        set => SetInt(_data, "InnerType", value);
    }

    // 强类型
    public int Cost
    {
        get => GetInt(_data, "Cost", 1);
        set => SetInt(_data, "Cost", value);
    }

    public Rarity Rarity
    {
        get => GetEnum(_data, "Rarity", Rarity.Common);
        set => SetEnum(_data, "Rarity", value);
    }

    public bool CanGivePassive
    {
        get => GetBool(_data, "CanGivePassive", true);
        set => SetBool(_data, "CanGivePassive", value);
    }

    public string Script
    {
        get => GetElementValue(_data, "Script");
        set => SetElementValue(_data, "Script", value);
    }

    public string Name
    {
        get => _text?.Element("Name")?.Value ?? "未翻译";
        set
        {
            if (IsVanilla) return;
            EnsureTextNode();
            if (_text != null) SetElementValue(_text, "Name", value);
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    public string Desc
    {
        get => _text?.Element("Desc")?.Value ?? "";
        set
        {
            if (IsVanilla) return;
            EnsureTextNode();
            if (_text != null) SetElementValue(_text, "Desc", value);
        }
    }

    private void EnsureTextNode()
    {
        if (_text != null || _textParent == null || IsVanilla) return;
        _text = new XElement("PassiveDesc", new XAttribute("ID", Id), new XElement("Name", "New Passive"),
            new XElement("Desc", "..."));
        _textParent.Add(_text);
    }

    public void DeleteXml()
    {
        if (IsVanilla) return;
        _data.Remove();
        _text?.Remove();
    }
}