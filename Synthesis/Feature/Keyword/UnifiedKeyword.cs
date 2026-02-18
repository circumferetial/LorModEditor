using System.Xml.Linq;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Attributes;

namespace Synthesis.Feature.Keyword;

public class UnifiedKeyword : XWrapper
{
    public UnifiedKeyword(XElement element, XElement? parent)
        : base(element)
    {
        Parent = parent;
        InitDefaults();
    }

    public XElement? Parent { get; }

    public string Id
    {
        get => GetAttr(Element, "ID");
        set
        {
            SetAttr(Element, "ID", value);
            OnPropertyChanged("DisplayName");
        }
    }

    public string Name
    {
        get => GetElementValue(Element, "Name", "New Keyword");
        set
        {
            SetElementValue(Element, "Name", value);
            OnPropertyChanged("DisplayName");
        }
    }

    public string Desc
    {
        get => GetElementValue(Element, "Desc", "Desc...");
        set => SetElementValue(Element, "Desc", value);
    }

    [NoAutoInit] public string DisplayName => Id + " - " + Name;

    public void DeleteXml()
    {
        if (!IsVanilla)
        {
            Element.Remove();
        }
    }
}
