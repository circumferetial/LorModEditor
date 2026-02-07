using System.Xml.Linq;
using Synthesis.Core.Abstraction;

namespace Synthesis.Feature.SkinEditor;

// 代表 XML 中的 <Default>, <Slash>, <Guard> 等单个节点
public class CharacterMotionData : XWrapper
{
    public CharacterMotionData(XElement element) : base(element)
    {
        // 确保 <Pivot> 子节点存在，否则读写会报错
        if (Element.Element("Pivot") == null)
        {
            Element.Add(new XElement("Pivot"));
        }
    }

    // 显示给 UI 列表用的名字 (例如 "Slash", "Default")
    public string MotionName => Element.Name.LocalName;

    // --- 核心属性：尺寸与质量 ---

    public double SizeX
    {
        get => GetDoubleAttr(Element, "size_x", 512);
        set => SetDoubleAttr(Element, "size_x", value);
    }

    public double SizeY
    {
        get => GetDoubleAttr(Element, "size_y", 512);
        set => SetDoubleAttr(Element, "size_y", value);
    }

    public int Quality
    {
        get => GetIntAttr(Element, "quality", 50);
        set => SetIntAttr(Element, "quality", value);
    }

    // --- 核心属性：锚点 (Pivot) ---
    // 注意：数据存在子节点 <Pivot pivot_x="..." /> 中
        
    public double PivotX
    {
        get => GetDoubleAttr(Element.Element("Pivot"), "pivot_x", 0.5);
        set => SetDoubleAttr(Element.Element("Pivot"), "pivot_x", value);
    }

    public double PivotY
    {
        get => GetDoubleAttr(Element.Element("Pivot"), "pivot_y");
        set => SetDoubleAttr(Element.Element("Pivot"), "pivot_y", value);
    }
}