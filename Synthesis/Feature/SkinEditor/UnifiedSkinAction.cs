using System.Xml.Linq;
using Synthesis.Core.Abstraction;

namespace Synthesis.Feature.OldSkinEditor;

public class UnifiedSkinAction(XElement element) : XWrapper(element)
{
    public string ActionName { get; } = element.Name.LocalName;

    // --- 基础属性 ---
    public string Direction
    {
        get => GetElementValue(Element, "Direction", "Front");
        set => SetElementValue(Element, "Direction", value);
    }

    // --- BaseMod Extended 扩展属性 (用于高清支持) ---
    // 默认 512，如果导入大图会自动变大
    public int SizeX
    {
        get => GetIntAttr(Element, "size_x", 512);
        set => SetIntAttr(Element, "size_x", value);
    }

    public int SizeY
    {
        get => GetIntAttr(Element, "size_y", 512);
        set => SetIntAttr(Element, "size_y", value);
    }

    // 图片质量 (影响缩放倍率，50 = 2倍，100 = 1倍)
    public int Quality
    {
        get => GetIntAttr(Element, "quality", 50);
        set => SetIntAttr(Element, "quality", value);
    }

    // --- 身体 Pivot ---
    private XElement PivotNode
    {
        get
        {
            var n = Element.Element("Pivot");
            if (n == null)
            {
                n = new XElement("Pivot");
                Element.Add(n);
            }
            return n;
        }
    }

    public string PivotX
    {
        get => GetAttr(PivotNode, "pivot_x", "0");
        set => SetAttr(PivotNode, "pivot_x", value);
    }

    public string PivotY
    {
        get => GetAttr(PivotNode, "pivot_y", "0");
        set => SetAttr(PivotNode, "pivot_y", value);
    }

    // --- 头部 Head ---
    private XElement HeadNode
    {
        get
        {
            var n = Element.Element("Head");
            if (n != null) return n;
            n = new XElement("Head");
            Element.Add(n);
            return n;
        }
    }

    public string HeadX
    {
        get => GetAttr(HeadNode, "head_x", "0");
        set => SetAttr(HeadNode, "head_x", value);
    }

    public string HeadY
    {
        get => GetAttr(HeadNode, "head_y", "0");
        set => SetAttr(HeadNode, "head_y", value);
    }

    public string HeadRotation
    {
        get => GetAttr(HeadNode, "rotation", "0");
        set => SetAttr(HeadNode, "rotation", value);
    }

    public bool HeadEnable
    {
        get => GetBoolAttr(HeadNode, "head_enable", true);
        set => SetBoolAttr(HeadNode, "head_enable", value);
    }
}