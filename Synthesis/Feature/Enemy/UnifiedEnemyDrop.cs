using System.Xml.Linq;
using Synthesis.Core;
using Synthesis.Core.Abstraction;

namespace Synthesis.Feature.Enemy;

public class UnifiedEnemyDrop : XWrapper
{
    public UnifiedEnemyDrop(XElement element) : base(element)
    {
        InitDefaults();
    }

    // 掉落概率 (通常是小数或整数权重，这里用 double 比较通用)
    // 比如 0.5 或 50
    public string Prob
    {
        // 建议保持 string 以支持 "0.5" 这种写法，或者用 double
        get => GetAttr(Element, "Prob", "1");
        set => SetAttr(Element, "Prob", value);
    }

    // 掉落的书籍 ID (可能是原版书，所以用 LorId)
    public LorId BookId
    {
        get => LorId.ParseXmlReference(Element, GlobalId.PackageId);
        set
        {
            if (IsVanilla) return;
            Element.Value = value.ItemId;
            if (value.PackageId != GlobalId.PackageId) Element.SetAttributeValue("Pid", value.PackageId);
            else Element.Attribute("Pid")?.Remove();
            OnPropertyChanged();
        }
    }
}