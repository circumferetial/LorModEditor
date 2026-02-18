using System.Collections.ObjectModel;
using System.Xml.Linq;
using Synthesis.Core;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Attributes;

namespace Synthesis.Feature.Enemy;

public class UnifiedEnemy : XWrapper
{
    private readonly XElement? _deckParent;

    private readonly XElement? _textParent;

    private readonly XElement _unitData;

    private XElement? _deckData;

    private XElement? _text;

    public UnifiedEnemy(XElement unit, XElement? deck, XElement? text, XElement? tp, XElement? dp)
        : base(unit)
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

    public string Id
    {
        get => GetAttr(_unitData, "ID");
        set
        {
            SetAttr(_unitData, "ID", value);
            if (_deckData != null && !IsVanilla)
            {
                SetAttr(_deckData, "ID", value);
            }
            SetElementValue(_unitData, "NameID", value);
            OnPropertyChanged("DisplayName");
            OnPropertyChanged("GlobalId");
        }
    }

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

    public LorId BookId
    {
        get => GetLorId(_unitData, "BookId");
        set => SetLorId(_unitData, "BookId", value);
    }

    private XElement DropTableNode
    {
        get
        {
            var xElement = _unitData.Element("DropTable");
            if (xElement == null)
            {
                xElement = new XElement("DropTable", new XAttribute("Level", "0"));
                _unitData.Add(xElement);
            }
            return xElement;
        }
    }

    public int DropLevel
    {
        get => GetInt(DropTableNode, "Level");
        set => SetInt(DropTableNode, "Level", value);
    }

    public ObservableCollection<UnifiedEnemyDrop> Drops { get; } = [];

    public ObservableCollection<LorId> DeckCardIds { get; } = [];

    private void LoadDrops()
    {
        Drops.Clear();
        if (_unitData.Element("DropTable") == null)
        {
            return;
        }
        foreach (var item in DropTableNode.Elements("DropItem"))
        {
            Drops.Add(new UnifiedEnemyDrop(item));
        }
    }

    public void AddDrop(LorId bookId)
    {
        if (!IsVanilla)
        {
            var xElement = new XElement("DropItem", new XAttribute("Prob", "1"), bookId.ItemId);
            if (bookId.PackageId != GlobalId.PackageId)
            {
                xElement.SetAttributeValue("Pid", bookId.PackageId);
            }
            DropTableNode.Add(xElement);
            Drops.Add(new UnifiedEnemyDrop(xElement));
        }
    }

    public void RemoveDrop(UnifiedEnemyDrop i)
    {
        if (!IsVanilla)
        {
            i.Element.Remove();
            Drops.Remove(i);
        }
    }

    private void LoadDeckCards()
    {
        DeckCardIds.Clear();
        if (_deckData == null)
        {
            return;
        }
        foreach (var item in _deckData.Elements("Card"))
        {
            DeckCardIds.Add(LorId.ParseXmlReference(item, GlobalId.PackageId));
        }
    }

    public void AddCardToDeck(LorId cid)
    {
        if (IsVanilla)
        {
            return;
        }
        EnsureDeckNode();
        if (_deckData != null)
        {
            var xElement = new XElement("Card", cid.ItemId);
            if (cid.PackageId != GlobalId.PackageId)
            {
                xElement.SetAttributeValue("Pid", cid.PackageId);
            }
            _deckData.Add(xElement);
            DeckCardIds.Add(cid);
        }
    }

    public void RemoveCardFromDeck(LorId cid)
    {
        if (!IsVanilla && _deckData != null)
        {
            _deckData.Elements("Card").FirstOrDefault(delegate(XElement x)
            {
                var a = x.Attribute("Pid")?.Value ?? GlobalId.PackageId;
                var value = x.Value;
                return string.Equals(a, cid.PackageId, StringComparison.OrdinalIgnoreCase) &&
                       string.Equals(value, cid.ItemId, StringComparison.OrdinalIgnoreCase);
            })?.Remove();
            DeckCardIds.Remove(cid);
        }
    }

    private void EnsureTextNode()
    {
        if (_text == null && _textParent != null && !IsVanilla)
        {
            _text = new XElement("Name", new XAttribute("ID", NameId), "New Enemy");
            _textParent.Add(_text);
        }
    }

    private void EnsureDeckNode()
    {
        if (_deckData == null && _deckParent != null && !IsVanilla)
        {
            _deckData = new XElement("Deck", new XAttribute("ID", Id));
            _deckParent.Add(_deckData);
        }
    }

    public void DeleteXml()
    {
        if (!IsVanilla)
        {
            _unitData.Remove();
            _deckData?.Remove();
            _text?.Remove();
        }
    }
}
