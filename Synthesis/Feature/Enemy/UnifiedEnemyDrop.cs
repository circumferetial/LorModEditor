using System.Xml.Linq;
using Synthesis.Core;
using Synthesis.Core.Abstraction;

namespace Synthesis.Feature.Enemy;

public class UnifiedEnemyDrop : XWrapper
{
    public UnifiedEnemyDrop(XElement element)
        : base(element)
    {
        InitDefaults();
    }

    public string Prob
    {
        get => GetAttr(Element, "Prob", "1");
        set => SetAttr(Element, "Prob", value);
    }

    public LorId BookId
    {
        get => LorId.ParseXmlReference(Element, GlobalId.PackageId);
        set
        {
            if (!IsVanilla)
            {
                Element.Value = value.ItemId;
                if (value.PackageId != GlobalId.PackageId)
                {
                    Element.SetAttributeValue("Pid", value.PackageId);
                }
                else
                {
                    Element.Attribute("Pid")?.Remove();
                }
                OnPropertyChanged();
            }
        }
    }
}
