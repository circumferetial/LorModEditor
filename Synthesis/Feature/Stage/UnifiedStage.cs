using System.Collections.ObjectModel;
using System.Xml.Linq;
using Synthesis.Core;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Attributes;
using Synthesis.Core.Enums;

namespace Synthesis.Feature.Stage;

public class UnifiedStage : XWrapper
{
    private readonly XElement _data;

    private readonly XElement? _textParent;

    private XElement? _text;

    public UnifiedStage(XElement data, XElement? text, XElement? textParent)
        : base(data)
    {
        _data = data;
        _text = text;
        _textParent = textParent;
        LoadWaves();
        LoadInvitationBooks();
        InitDefaults();
    }

    [NoAutoInit] public string DisplayName => $"{GlobalId} {Name}";

    public string Id
    {
        get => GetAttr(_data, "id");
        set
        {
            SetAttr(_data, "id", value);
            if (_text != null && !IsVanilla)
            {
                SetAttr(_text, "ID", value);
            }
            OnPropertyChanged("DisplayName");
        }
    }

    public string Name
    {
        get => _text?.Value ?? "未翻译";
        set
        {
            if (!IsVanilla)
            {
                EnsureTextNode();
                var text = _text;
                if (text != null)
                {
                    text.Value = value;
                }
                OnPropertyChanged("DisplayName");
            }
        }
    }

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

    private XElement InvNode
    {
        get
        {
            var xElement = _data.Element("Invitation");
            if (xElement == null)
            {
                xElement = new XElement("Invitation");
                _data.Add(xElement);
            }
            return xElement;
        }
    }

    public InvitationCombine InvitationCombine
    {
        get => GetEnumAttr(InvNode, "Combine", InvitationCombine.BookValue);
        set
        {
            SetEnumAttr(InvNode, "Combine", value);
            OnPropertyChanged();
            OnPropertyChanged("IsBookValueMode");
            OnPropertyChanged("IsBookRecipeMode");
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

    public ObservableCollection<LorId> InvitationBooks { get; } = [];

    public ObservableCollection<UnifiedWave> Waves { get; } = [];

    private void LoadInvitationBooks()
    {
        InvitationBooks.Clear();
        if (_data.Element("Invitation") == null)
        {
            return;
        }
        foreach (var item in InvNode.Elements("Book"))
        {
            InvitationBooks.Add(LorId.ParseXmlReference(item, GlobalId.PackageId));
        }
    }

    public void AddInvitationBook(LorId bid)
    {
        if (!IsVanilla)
        {
            var xElement = new XElement("Book", bid.ItemId);
            if (bid.PackageId != GlobalId.PackageId)
            {
                xElement.SetAttributeValue("Pid", bid.PackageId);
            }
            InvNode.Add(xElement);
            InvitationBooks.Add(bid);
        }
    }

    public void RemoveInvitationBook(LorId bid)
    {
        if (!IsVanilla)
        {
            InvNode.Elements("Book").FirstOrDefault(x =>
                (x.Attribute("Pid")?.Value ?? GlobalId.PackageId) == bid.PackageId && x.Value == bid.ItemId)?.Remove();
            InvitationBooks.Remove(bid);
        }
    }

    private void LoadWaves()
    {
        Waves.Clear();
        foreach (var item in _data.Elements("Wave"))
        {
            Waves.Add(new UnifiedWave(item));
        }
    }

    public void AddWave()
    {
        if (!IsVanilla)
        {
            var xElement = new XElement("Wave");
            xElement.Add(new XElement("Formation", 1));
            xElement.Add(new XElement("AvailableUnit", 5));
            _data.Add(xElement);
            Waves.Add(new UnifiedWave(xElement));
        }
    }

    public void RemoveWave(UnifiedWave w)
    {
        if (!IsVanilla)
        {
            w.Element.Remove();
            Waves.Remove(w);
        }
    }

    private void EnsureTextNode()
    {
        if (_text == null && _textParent != null && !IsVanilla)
        {
            _text = new XElement("Name", new XAttribute("ID", Id), "New Stage");
            _textParent.Add(_text);
        }
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
