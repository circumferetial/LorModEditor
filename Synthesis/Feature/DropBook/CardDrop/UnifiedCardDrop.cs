using System.Collections.ObjectModel;
using System.Xml.Linq;
using Synthesis.Core;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Attributes;

namespace Synthesis.Feature.DropBook.CardDrop;

public class UnifiedCardDrop : XWrapper
{
    public UnifiedCardDrop(XElement element)
        : base(element)
    {
        LoadCards();
        InitDefaults();
    }

    public string BookId
    {
        get => GetAttr(Element, "ID");
        set
        {
            SetAttr(Element, "ID", value);
            OnPropertyChanged("DisplayName");
        }
    }

    [NoAutoInit] public string DisplayName => "DropTable For Book [" + BookId + "]";

    public ObservableCollection<LorId> CardIds { get; } = [];

    private void LoadCards()
    {
        CardIds.Clear();
        foreach (var item in Element.Elements("Card"))
        {
            CardIds.Add(LorId.ParseXmlReference(item, GlobalId.PackageId));
        }
    }

    public void AddCard(LorId cid)
    {
        if (!IsVanilla)
        {
            var xElement = new XElement("Card", cid.ItemId);
            if (cid.PackageId != GlobalId.PackageId)
            {
                xElement.SetAttributeValue("Pid", cid.PackageId);
            }
            Element.Add(xElement);
            CardIds.Add(cid);
        }
    }

    public void RemoveCard(LorId cid)
    {
        if (!IsVanilla)
        {
            Element.Elements("Card").FirstOrDefault(x =>
                (x.Attribute("Pid")?.Value ?? GlobalId.PackageId) == cid.PackageId && x.Value == cid.ItemId)?.Remove();
            CardIds.Remove(cid);
        }
    }

    public void DeleteXml()
    {
        if (!IsVanilla)
        {
            Element.Remove();
        }
    }
}
