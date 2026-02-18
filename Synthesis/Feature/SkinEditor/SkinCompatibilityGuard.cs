using System.IO;
using System.Xml.Linq;
using Synthesis.Core.Enums;
using Synthesis.Core.Tools;

namespace Synthesis.Feature.SkinEditor;

internal static class SkinCompatibilityGuard
{
    private static readonly HashSet<string> NonActionTags = new(StringComparer.OrdinalIgnoreCase)
        { "Name", "SoundList", "AtkEffectPivotInfo", "SpecialMotionPivotInfo", "FaceInfo", "ExtendedFaceInfo" };

    private static readonly string[] CopyImageSuffixes =
    [
        "", "_skin", "_mid", "_mid_skin", "_back", "_back_skin", "_front", "_front_skin", "_large", "_front_large",
        "_effect"
    ];

    public static IReadOnlyList<ActionDetail> PresetActions { get; } = Array.AsReadOnly([
        ActionDetail.Default,
        ActionDetail.Guard,
        ActionDetail.Evade,
        ActionDetail.Damaged,
        ActionDetail.Slash,
        ActionDetail.Penetrate,
        ActionDetail.Hit,
        ActionDetail.Move,
        ActionDetail.Fire,
        ActionDetail.Aim,
        ActionDetail.Special,
        ActionDetail.S1,
        ActionDetail.S2,
        ActionDetail.S3,
        ActionDetail.S4,
        ActionDetail.S5,
        ActionDetail.S6,
        ActionDetail.S7,
        ActionDetail.S8,
        ActionDetail.S9,
        ActionDetail.S10,
        ActionDetail.S11,
        ActionDetail.S12,
        ActionDetail.S13,
        ActionDetail.S14,
        ActionDetail.S15,
        ActionDetail.Slash2,
        ActionDetail.Penetrate2,
        ActionDetail.Hit2
    ]);

    public static IReadOnlyList<SkinCompatibilityIssue> Validate(XElement modInfoRoot)
    {
        var list = new List<SkinCompatibilityIssue>();
        var xElement = modInfoRoot.Element("ClothInfo");
        if (xElement == null)
        {
            return list;
        }
        var skinName = ResolveSkinName(modInfoRoot, xElement);
        var list2 = GetActionNodes(xElement).ToList();
        var flag = list2.Any(n => IsAction(n, ActionDetail.Default));
        if (list2.Count > 0 && !flag)
        {
            list.Add(new SkinCompatibilityIssue("SKIN_NO_DEFAULT", "缺少 Default 动作，导出后可能触发游戏 KeyNotFoundException。",
                SkinCompatibilitySeverity.Blocking, skinName, ActionDetail.Default.ToString()));
        }
        var flag2 = list2.Any(n => IsAction(n, ActionDetail.Penetrate));
        if (list2.Count > 0 && !flag2)
        {
            list.Add(new SkinCompatibilityIssue("SKIN_NO_PENETRATE", "缺少 Penetrate 动作，回退兼容性较弱。",
                SkinCompatibilitySeverity.Warning, skinName, ActionDetail.Penetrate.ToString()));
        }
        foreach (var item in list2)
        {
            var localName = item.Name.LocalName;
            if (string.Equals(localName, ActionDetail.Standing.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new SkinCompatibilityIssue("SKIN_STANDING_IGNORED", "存在 Standing 节点，加载端会忽略该动作。",
                    SkinCompatibilitySeverity.Warning, skinName, localName));
                continue;
            }
            var result = ActionDetail.NONE;
            if (string.Equals(localName, result.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                list.Add(new SkinCompatibilityIssue("SKIN_NONE_IGNORED", "存在 NONE 节点，加载端会忽略该动作。",
                    SkinCompatibilitySeverity.Warning, skinName, localName));
            }
            else if (!Enum.TryParse(localName, true, out result))
            {
                list.Add(new SkinCompatibilityIssue("SKIN_UNKNOWN_ACTION", "存在非 ActionDetail 的动作节点，当前主流程不会消费。",
                    SkinCompatibilitySeverity.Warning, skinName, localName));
            }
        }
        return list;
    }

    public static SkinFixResult ApplyFix(XElement modInfoRoot, SkinFixPolicy policy)
    {
        var xElement = modInfoRoot.Element("ClothInfo");
        var text = ResolveSkinName(modInfoRoot, xElement);
        var skinFixResult = new SkinFixResult(text);
        if (xElement == null)
        {
            return skinFixResult;
        }
        var list = GetActionNodes(xElement).ToList();
        if (list.Count == 0)
        {
            return skinFixResult;
        }
        if (list.Any(n => IsAction(n, ActionDetail.Default)))
        {
            return skinFixResult;
        }
        XElement xElement2 = null;
        string actionName = null;
        if (policy == SkinFixPolicy.PreferPenetrateForDefault)
        {
            xElement2 = FindActionNode(xElement, ActionDetail.Penetrate.ToString());
            actionName = xElement2?.Name.LocalName;
        }
        if (xElement2 == null)
        {
            xElement2 = FindFirstCompatibleSource(xElement, out actionName);
        }
        XElement xElement3;
        if (xElement2 != null)
        {
            xElement3 = new XElement(ActionDetail.Default.ToString(), xElement2.Attributes(), xElement2.Nodes());
            skinFixResult.Messages.Add($"[{text}] 已从 {actionName} 克隆 Default。");
        }
        else
        {
            xElement3 = CreateStandardActionNode(ActionDetail.Default.ToString());
            skinFixResult.Messages.Add("[" + text + "] 已创建标准 Default 模板。");
        }
        EnsureStandardActionShape(xElement3);
        xElement.Add(xElement3);
        skinFixResult.HasChanges = true;
        if (!string.IsNullOrWhiteSpace(actionName) && !string.Equals(actionName, ActionDetail.Default.ToString(),
                StringComparison.OrdinalIgnoreCase))
        {
            CopyActionImages(modInfoRoot, actionName, ActionDetail.Default.ToString(), skinFixResult);
        }
        return skinFixResult;
    }

    public static XElement CreateStandardActionNode(string actionName)
    {
        var xElement = new XElement(actionName);
        EnsureStandardActionShape(xElement);
        return xElement;
    }

    private static IEnumerable<XElement> GetActionNodes(XElement clothInfo) => from node in clothInfo.Elements()
        where !NonActionTags.Contains(node.Name.LocalName)
        select node;

    private static bool IsAction(XElement node, ActionDetail actionDetail) => string.Equals(node.Name.LocalName,
        actionDetail.ToString(), StringComparison.OrdinalIgnoreCase);

    private static XElement? FindActionNode(XElement clothInfo, string actionName)
    {
        return GetActionNodes(clothInfo).FirstOrDefault(n =>
            string.Equals(n.Name.LocalName, actionName, StringComparison.OrdinalIgnoreCase));
    }

    private static XElement? FindFirstCompatibleSource(XElement clothInfo, out string? actionName)
    {
        foreach (var presetAction in PresetActions)
        {
            if (presetAction != ActionDetail.Default)
            {
                var xElement = FindActionNode(clothInfo, presetAction.ToString());
                if (xElement != null)
                {
                    actionName = xElement.Name.LocalName;
                    return xElement;
                }
            }
        }
        actionName = null;
        return null;
    }

    private static void EnsureStandardActionShape(XElement actionNode)
    {
        var xElement = actionNode.Element("Direction");
        if (xElement == null)
        {
            actionNode.AddFirst(new XElement("Direction", "Front"));
        }
        else if (string.IsNullOrWhiteSpace(xElement.Value))
        {
            xElement.Value = "Front";
        }
        EnsureAttr(actionNode, "size_x", "512");
        EnsureAttr(actionNode, "size_y", "512");
        EnsureAttr(actionNode, "quality", "50");
        var xElement2 = actionNode.Element("Pivot");
        if (xElement2 == null)
        {
            xElement2 = new XElement("Pivot");
            actionNode.Add(xElement2);
        }
        EnsureAttr(xElement2, "pivot_x", "0");
        EnsureAttr(xElement2, "pivot_y", "0");
        var xElement3 = actionNode.Element("Head");
        if (xElement3 == null)
        {
            xElement3 = new XElement("Head");
            actionNode.Add(xElement3);
        }
        EnsureAttr(xElement3, "head_x", "0");
        EnsureAttr(xElement3, "head_y", "0");
        EnsureAttr(xElement3, "head_enable", "true");
    }

    private static void EnsureAttr(XElement node, string attrName, string defaultValue)
    {
        if (node.Attribute(attrName) == null)
        {
            node.SetAttributeValue(attrName, defaultValue);
        }
    }

    private static void CopyActionImages(XElement modInfoRoot, string sourceAction, string targetAction,
        SkinFixResult result)
    {
        try
        {
            var text = modInfoRoot.Annotation<FilePathAnnotation>()?.Path;
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }
            var directoryName = Path.GetDirectoryName(text);
            if (string.IsNullOrWhiteSpace(directoryName))
            {
                return;
            }
            var text2 = Path.Combine(directoryName, "ClothCustom");
            if (!Directory.Exists(text2))
            {
                return;
            }
            var copyImageSuffixes = CopyImageSuffixes;
            foreach (var text3 in copyImageSuffixes)
            {
                var text4 = Path.Combine(text2, sourceAction + text3 + ".png");
                if (File.Exists(text4))
                {
                    var text5 = Path.Combine(text2, targetAction + text3 + ".png");
                    File.Copy(text4, text5, true);
                    result.HasChanges = true;
                    result.Messages.Add(
                        $"[{result.SkinName}] 图片迁移: {Path.GetFileName(text4)} -> {Path.GetFileName(text5)}");
                }
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add("[" + result.SkinName + "] 迁移动作图片失败: " + ex.Message);
        }
    }

    private static string ResolveSkinName(XElement modInfoRoot, XElement? clothInfo)
    {
        var text = clothInfo?.Element("Name")?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(text))
        {
            return text;
        }
        var text2 = modInfoRoot.Annotation<FilePathAnnotation>()?.Path;
        if (!string.IsNullOrWhiteSpace(text2))
        {
            var directoryName = Path.GetDirectoryName(text2);
            if (!string.IsNullOrWhiteSpace(directoryName))
            {
                return Path.GetFileName(directoryName);
            }
        }
        return "UnknownSkin";
    }
}
