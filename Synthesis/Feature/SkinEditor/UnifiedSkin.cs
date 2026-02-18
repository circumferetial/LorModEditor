using System.Collections.ObjectModel;
using System.Xml;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;

namespace Synthesis.Feature.SkinEditor;

public class UnifiedSkin : XWrapper
{
    private static readonly HashSet<string> NonActionTags =
        ["Name", "SoundList", "AtkEffectPivotInfo", "SpecialMotionPivotInfo", "FaceInfo", "ExtendedFaceInfo"];

    public UnifiedSkin(XElement element)
        : base(element)
    {
        LoadActions();
        var xElement = ClothInfoNode.Element("SoundList");
        if (xElement == null)
        {
            xElement = new XElement("SoundList");
            ClothInfoNode.Add(xElement);
        }
    }

    public string DisplayName => "Skin: " + Name;

    private XElement ClothInfoNode
    {
        get
        {
            var xElement = Element.Element("ClothInfo");
            if (xElement == null)
            {
                xElement = new XElement("ClothInfo");
                Element.Add(xElement);
            }
            return xElement;
        }
    }

    public string Name
    {
        get => ClothInfoNode.Element("Name")?.Value ?? "NewSkin";
        set
        {
            var xElement = ClothInfoNode.Element("Name");
            if (xElement == null)
            {
                xElement = new XElement("Name");
                ClothInfoNode.Add(xElement);
            }
            xElement.Value = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<UnifiedSkinAction> Actions { get; } = [];

    public bool IsExtended
    {
        get => GetBoolAttr(Element, "Extended", true);
        set => SetBoolAttr(Element, "Extended", value);
    }

    private void LoadActions()
    {
        Actions.Clear();
        foreach (var item in ClothInfoNode.Elements())
        {
            var localName = item.Name.LocalName;
            if (!NonActionTags.Contains(localName))
            {
                Actions.Add(new UnifiedSkinAction(item));
            }
        }
    }

    public bool AddAction(string actionName, out UnifiedSkinAction? createdAction, out string? errorMessage)
    {
        createdAction = null;
        errorMessage = null;
        if (IsVanilla)
        {
            errorMessage = "原版皮肤不可编辑。";
            return false;
        }
        var normalizedName = actionName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            errorMessage = "动作名称不能为空。";
            return false;
        }
        try
        {
            XmlConvert.VerifyName(normalizedName);
        }
        catch (Exception ex)
        {
            errorMessage = "动作名不合法: " + ex.Message;
            return false;
        }
        if (Actions.Any(x => x.ActionName.Equals(normalizedName, StringComparison.OrdinalIgnoreCase)))
        {
            errorMessage = "该动作已存在！";
            return false;
        }
        var xElement = new XElement(normalizedName);
        xElement.Add(new XElement("Direction", "Front"));
        xElement.Add(new XAttribute("size_x", "512"));
        xElement.Add(new XAttribute("size_y", "512"));
        xElement.Add(new XAttribute("quality", "50"));
        xElement.Add(new XElement("Pivot", new XAttribute("pivot_x", "0"), new XAttribute("pivot_y", "0")));
        xElement.Add(new XElement("Head", new XAttribute("head_x", "0"), new XAttribute("head_y", "0"),
            new XAttribute("head_enable", "true")));
        ClothInfoNode.Add(xElement);
        createdAction = new UnifiedSkinAction(xElement);
        Actions.Add(createdAction);
        return true;
    }

    public bool RemoveAction(UnifiedSkinAction action)
    {
        if (IsVanilla)
        {
            return false;
        }
        var unifiedSkinAction = Actions.FirstOrDefault(x =>
            x == action || x.ActionName.Equals(action.ActionName, StringComparison.OrdinalIgnoreCase));
        if (unifiedSkinAction == null)
        {
            return false;
        }
        unifiedSkinAction.Element.Remove();
        return Actions.Remove(unifiedSkinAction);
    }

    public void DeleteXml()
    {
        Element.Remove();
    }
}
