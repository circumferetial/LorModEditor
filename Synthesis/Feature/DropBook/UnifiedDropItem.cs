using System.Xml.Linq;
using JetBrains.Annotations;
using Synthesis.Core;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Enums;

namespace Synthesis.Feature.DropBook;

public class UnifiedDropItem : XWrapper
{
    public UnifiedDropItem(XElement element) : base(element)
    {
        InitDefaults();
    }

    // 1. 强类型枚举 (Equip / Card)
    [UsedImplicitly]
    public DropItemType Type
    {
        get => GetEnumAttr(Element, "Type", DropItemType.Equip);
        set => SetEnumAttr(Element, "Type", value);
    }

    // 2. 强类型 LorId (处理引用)
    // 掉落物可能是原版的，也可能是 Mod 的，所以必须处理 Pid
    public LorId ItemId
    {
        get => LorId.ParseXmlReference(Element, GlobalId.PackageId);

        set
        {
            if (IsVanilla) return;

            // 写入值
            Element.Value = value.ItemId;

            // 处理 Pid
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