using System.Collections.ObjectModel;
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

public class UnifiedSkin : XWrapper
{
    // 定义非动作节点的名称黑名单
    // 这些节点不会被当做“小人动作”加载到列表中
    private static readonly HashSet<string> NonActionTags =
    [
        "Name",
        "SoundList",
        "AtkEffectPivotInfo",
        "SpecialMotionPivotInfo",
        "FaceInfo",
        "ExtendedFaceInfo"
    ];

    public UnifiedSkin(XElement element) : base(element)
    {
        LoadActions();

        // 1. 初始化 SoundList (之前的代码)
        var soundNode = ClothInfoNode.Element("SoundList");
        if (soundNode == null)
        {
            soundNode = new XElement("SoundList");
            ClothInfoNode.Add(soundNode);
        }

    }

    public string DisplayName => $"Skin: {Name}";

    // 获取 ModInfo 下的 ClothInfo 节点
    private XElement ClothInfoNode
    {
        get
        {
            var n = Element.Element("ClothInfo");
            if (n == null)
            {
                n = new XElement("ClothInfo");
                Element.Add(n);
            }
            return n;
        }
    }

    public string Name
    {
        get => ClothInfoNode.Element("Name")?.Value ?? "NewSkin";
        set
        {
            var n = ClothInfoNode.Element("Name");
            if (n == null)
            {
                n = new XElement("Name");
                ClothInfoNode.Add(n);
            }
            n.Value = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<UnifiedSkinAction> Actions { get; } = new();
    

    private void LoadActions()
    {
        Actions.Clear();
        foreach (var node in ClothInfoNode.Elements())
        {
            var tagName = node.Name.LocalName;

            // 【关键修改】如果在这个黑名单里，就跳过
            if (NonActionTags.Contains(tagName)) continue;

            Actions.Add(new UnifiedSkinAction(node));
        }
    }

    public void AddAction(string actionName)
    {
        if (Actions.Any(x => x.ActionName == actionName)) return;

        var node = new XElement(actionName);
        node.Add(new XElement("Direction", "Front"));
        // 默认加上 size 和 quality
        node.Add(new XAttribute("size_x", "512"));
        node.Add(new XAttribute("size_y", "512"));
        node.Add(new XAttribute("quality", "50"));

        node.Add(new XElement("Pivot", new XAttribute("pivot_x", "0"), new XAttribute("pivot_y", "0")));
        node.Add(new XElement("Head", new XAttribute("head_x", "0"), new XAttribute("head_y", "0"),
            new XAttribute("head_enable", "true")));

        ClothInfoNode.Add(node);
        Actions.Add(new UnifiedSkinAction(node));
    }

    public void DeleteXml()
    {
        Element.Remove();
    }
}
