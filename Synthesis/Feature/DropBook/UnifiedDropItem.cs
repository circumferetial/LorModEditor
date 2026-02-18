using System.Xml.Linq;
using Synthesis.Core;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Enums;

namespace Synthesis.Feature.DropBook;

public class UnifiedDropItem : XWrapper
{
    public UnifiedDropItem(XElement element)
        : base(element)
    {
        InitDefaults();
    }

    public DropItemType Type
    {
        get => GetEnumAttr(Element, "Type", DropItemType.Equip);
        set => SetEnumAttr(Element, "Type", value);
    }

    public LorId ItemId
    {
        get => LorId.ParseXmlReference(Element, GlobalId.PackageId);
        set
        {
            if (!IsVanilla)
            {
                Element.Value = value.ItemId;
                if (value.PackageId != GlobalId.PackageId && !string.IsNullOrEmpty(value.PackageId))
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
