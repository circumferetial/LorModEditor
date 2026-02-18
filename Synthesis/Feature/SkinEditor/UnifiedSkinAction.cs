using System.Xml.Linq;
using Synthesis.Core.Abstraction;

namespace Synthesis.Feature.SkinEditor;

public class UnifiedSkinAction(XElement element) : XWrapper(element)
{
    public string ActionName { get; } = element.Name.LocalName;

    public string Direction
    {
        get => GetElementValue(Element, "Direction", "Front");
        set => SetElementValue(Element, "Direction", value);
    }

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

    public int Quality
    {
        get => GetIntAttr(Element, "quality", 50);
        set => SetIntAttr(Element, "quality", value);
    }

    private XElement PivotNode
    {
        get
        {
            var xElement = Element.Element("Pivot");
            if (xElement != null)
            {
                return xElement;
            }
            xElement = new XElement("Pivot");
            Element.Add(xElement);
            return xElement;
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

    private XElement HeadNode
    {
        get
        {
            var xElement = Element.Element("Head");
            if (xElement != null)
            {
                return xElement;
            }
            xElement = new XElement("Head");
            Element.Add(xElement);
            return xElement;
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
