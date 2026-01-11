using System.Collections.ObjectModel;
using System.Xml.Linq;
using LorModEditor.Core.Attributes;
using LorModEditor.Core.Enums;

// 确保引用

namespace LorModEditor.Core.Wrappers;

public class UnifiedWave : XWrapper
{
    public UnifiedWave(XElement element) : base(element)
    {
        LoadUnits();
        InitDefaults();
    }

    // 使用 GetInt (Element)
    public int FormationId
    {
        get => GetInt(Element, "Formation", 1);
        set => SetInt(Element, "Formation", value);
    }

    public int AvailableUnit
    {
        get => GetInt(Element, "AvailableUnit", 5);
        set => SetInt(Element, "AvailableUnit", value);
    }

    public string ManagerScript
    {
        get => GetElementValue(Element, "ManagerScript");
        set => SetElementValue(Element, "ManagerScript", value);
    }

    public ObservableCollection<LorId> Units { get; } = new();

    private void LoadUnits()
    {
        Units.Clear();
        foreach (var u in Element.Elements("Unit"))
            Units.Add(LorId.ParseXmlReference(u, GlobalId.PackageId));
    }

    public void AddUnit(LorId uid)
    {
        if (IsVanilla) return;
        var node = new XElement("Unit", uid.ItemId);
        if (uid.PackageId != GlobalId.PackageId) node.SetAttributeValue("Pid", uid.PackageId);
        Element.Add(node);
        Units.Add(uid);
    }

    public void RemoveUnit(LorId uid)
    {
        if (IsVanilla) return;
        var node = Element.Elements("Unit").FirstOrDefault(x =>
            (x.Attribute("Pid")?.Value ?? GlobalId.PackageId) == uid.PackageId && x.Value == uid.ItemId);
        node?.Remove();
        Units.Remove(uid);
    }
}

public class UnifiedStage : XWrapper
{
    private readonly XElement _data;
    private readonly XElement? _textParent;
    private XElement? _text;

    public UnifiedStage(XElement data, XElement? text, XElement? textParent) : base(data)
    {
        _data = data;
        _text = text;
        _textParent = textParent;
        LoadWaves();
        LoadInvitationBooks();
        InitDefaults();
    }

    [NoAutoInit] public string DisplayName => $"{GlobalId} {Name}";

    // ID 是属性 (注意 StageInfo 里通常是小写 id)
    public string Id
    {
        get => GetAttr(_data, "id");// 我们的基类应该已经处理了大小写，如果没有，这里最关键
        set
        {
            SetAttr(_data, "id", value);
            if (_text != null && !IsVanilla) SetAttr(_text, "ID", value);
            OnPropertyChanged(nameof(DisplayName));
        }
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

    // 这些必须是 Element (GetInt/SetInt)
    public int FloorNum
    {
        get => GetInt(_data, "FloorNum", 1);
        set => SetInt(_data, "FloorNum", value);
    }

    public int Chapter
    {
        get => GetInt(_data, "Chapter", 1);
        set => SetInt(_data, "Chapter", value);
    }

    public string StoryType
    {
        get => GetElementValue(_data, "StoryType");
        set => SetElementValue(_data, "StoryType", value);
    }

    // --- Invitation ---
    private XElement InvNode
    {
        get
        {
            var n = _data.Element("Invitation");
            if (n == null)
            {
                n = new XElement("Invitation");
                _data.Add(n);
            }
            return n;
        }
    }

    public InvitationCombine InvitationCombine
    {
        get => GetEnumAttr(InvNode, "Combine", InvitationCombine.BookValue);
        set
        {
            SetEnumAttr(InvNode, "Combine", value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsBookValueMode));
            OnPropertyChanged(nameof(IsBookRecipeMode));
        }
    }

    public bool IsBookValueMode => InvitationCombine == InvitationCombine.BookValue;
    public bool IsBookRecipeMode => InvitationCombine == InvitationCombine.BookRecipe;

    public int InvitationValue
    {
        get => GetInt(InvNode, "Value");
        set => SetInt(InvNode, "Value", value);
    }

    public int InvitationNum
    {
        get => GetInt(InvNode, "Num", 1);
        set => SetInt(InvNode, "Num", value);
    }

    // --- Invitation Books ---
    public ObservableCollection<LorId> InvitationBooks { get; } = new();

    // --- Waves ---
    public ObservableCollection<UnifiedWave> Waves { get; } = new();

    private void LoadInvitationBooks()
    {
        InvitationBooks.Clear();
        if (_data.Element("Invitation") == null) return;
        foreach (var b in InvNode.Elements("Book")) InvitationBooks.Add(LorId.ParseXmlReference(b, GlobalId.PackageId));
    }

    public void AddInvitationBook(LorId bid)
    {
        if (IsVanilla) return;
        var node = new XElement("Book", bid.ItemId);
        if (bid.PackageId != GlobalId.PackageId) node.SetAttributeValue("Pid", bid.PackageId);
        InvNode.Add(node);
        InvitationBooks.Add(bid);
    }

    public void RemoveInvitationBook(LorId bid)
    {
        if (IsVanilla) return;
        var node = InvNode.Elements("Book").FirstOrDefault(x =>
            (x.Attribute("Pid")?.Value ?? GlobalId.PackageId) == bid.PackageId && x.Value == bid.ItemId);
        node?.Remove();
        InvitationBooks.Remove(bid);
    }

    private void LoadWaves()
    {
        Waves.Clear();
        foreach (var w in _data.Elements("Wave")) Waves.Add(new UnifiedWave(w));
    }

    public void AddWave()
    {
        if (IsVanilla) return;
        var w = new XElement("Wave");
        w.Add(new XElement("Formation", 1));
        w.Add(new XElement("AvailableUnit", 5));
        _data.Add(w);
        Waves.Add(new UnifiedWave(w));
    }

    public void RemoveWave(UnifiedWave w)
    {
        if (IsVanilla) return;
        w.Element.Remove();
        Waves.Remove(w);
    }

    private void EnsureTextNode()
    {
        if (_text != null || _textParent == null || IsVanilla) return;
        _text = new XElement("Name", new XAttribute("ID", Id), "New Stage");
        _textParent.Add(_text);
    }

    public void DeleteXml()
    {
        if (IsVanilla) return;
        _data.Remove();
        _text?.Remove();
    }
}