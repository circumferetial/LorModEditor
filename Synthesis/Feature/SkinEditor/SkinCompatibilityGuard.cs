using System.IO;
using System.Xml.Linq;
using Synthesis.Core.Enums;
using Synthesis.Core.Tools;

namespace Synthesis.Feature.SkinEditor;

internal enum SkinCompatibilitySeverity
{
    Warning,
    Blocking
}

internal enum SkinFixPolicy
{
    PreferPenetrateForDefault
}

internal sealed class SkinCompatibilityIssue(
    string code,
    string message,
    SkinCompatibilitySeverity severity,
    string skinName,
    string? actionName = null)
{
    public string Code { get; } = code;
    public string Message { get; } = message;
    public SkinCompatibilitySeverity Severity { get; } = severity;
    public string SkinName { get; } = skinName;
    public string? ActionName { get; } = actionName;
}

internal sealed class SkinFixResult(string skinName)
{
    public string SkinName { get; } = skinName;
    public bool HasChanges { get; set; }
    public List<string> Messages { get; } = [];
    public List<string> Errors { get; } = [];
}

internal static class SkinCompatibilityGuard
{
    private static readonly HashSet<string> NonActionTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "Name",
        "SoundList",
        "AtkEffectPivotInfo",
        "SpecialMotionPivotInfo",
        "FaceInfo",
        "ExtendedFaceInfo"
    };

    private static readonly string[] CopyImageSuffixes =
    [
        "",
        "_skin",
        "_mid",
        "_mid_skin",
        "_back",
        "_back_skin",
        "_front",
        "_front_skin",
        "_large",
        "_front_large",
        "_effect"
    ];

    public static IReadOnlyList<ActionDetail> PresetActions { get; } =
    [
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
    ];

    public static IReadOnlyList<SkinCompatibilityIssue> Validate(XElement modInfoRoot)
    {
        var issues = new List<SkinCompatibilityIssue>();
        var clothInfo = modInfoRoot.Element("ClothInfo");
        if (clothInfo == null) return issues;

        var skinName = ResolveSkinName(modInfoRoot, clothInfo);
        var actionNodes = GetActionNodes(clothInfo).ToList();

        var hasDefault = actionNodes.Any(n => IsAction(n, ActionDetail.Default));
        if (actionNodes.Count > 0 && !hasDefault)
        {
            issues.Add(new SkinCompatibilityIssue(
                "SKIN_NO_DEFAULT",
                "缺少 Default 动作，导出后可能触发游戏 KeyNotFoundException。",
                SkinCompatibilitySeverity.Blocking,
                skinName,
                ActionDetail.Default.ToString()));
        }

        var hasPenetrate = actionNodes.Any(n => IsAction(n, ActionDetail.Penetrate));
        if (actionNodes.Count > 0 && !hasPenetrate)
        {
            issues.Add(new SkinCompatibilityIssue(
                "SKIN_NO_PENETRATE",
                "缺少 Penetrate 动作，回退兼容性较弱。",
                SkinCompatibilitySeverity.Warning,
                skinName,
                ActionDetail.Penetrate.ToString()));
        }

        foreach (var actionNode in actionNodes)
        {
            var actionName = actionNode.Name.LocalName;
            if (string.Equals(actionName, ActionDetail.Standing.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new SkinCompatibilityIssue(
                    "SKIN_STANDING_IGNORED",
                    "存在 Standing 节点，加载端会忽略该动作。",
                    SkinCompatibilitySeverity.Warning,
                    skinName,
                    actionName));
                continue;
            }

            if (string.Equals(actionName, ActionDetail.NONE.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new SkinCompatibilityIssue(
                    "SKIN_NONE_IGNORED",
                    "存在 NONE 节点，加载端会忽略该动作。",
                    SkinCompatibilitySeverity.Warning,
                    skinName,
                    actionName));
                continue;
            }

            if (!Enum.TryParse<ActionDetail>(actionName, true, out _))
            {
                issues.Add(new SkinCompatibilityIssue(
                    "SKIN_UNKNOWN_ACTION",
                    "存在非 ActionDetail 的动作节点，当前主流程不会消费。",
                    SkinCompatibilitySeverity.Warning,
                    skinName,
                    actionName));
            }
        }

        return issues;
    }

    public static SkinFixResult ApplyFix(XElement modInfoRoot, SkinFixPolicy policy)
    {
        var clothInfo = modInfoRoot.Element("ClothInfo");
        var skinName = ResolveSkinName(modInfoRoot, clothInfo);
        var result = new SkinFixResult(skinName);
        if (clothInfo == null) return result;

        var actionNodes = GetActionNodes(clothInfo).ToList();
        if (actionNodes.Count == 0) return result;
        if (actionNodes.Any(n => IsAction(n, ActionDetail.Default))) return result;

        XElement? sourceNode = null;
        string? sourceActionName = null;

        if (policy == SkinFixPolicy.PreferPenetrateForDefault)
        {
            sourceNode = FindActionNode(clothInfo, ActionDetail.Penetrate.ToString());
            sourceActionName = sourceNode?.Name.LocalName;
        }

        if (sourceNode == null)
        {
            sourceNode = FindFirstCompatibleSource(clothInfo, out sourceActionName);
        }

        XElement defaultNode;
        if (sourceNode != null)
        {
            defaultNode = new XElement(ActionDetail.Default.ToString(), sourceNode.Attributes(), sourceNode.Nodes());
            result.Messages.Add($"[{skinName}] 已从 {sourceActionName} 克隆 Default。");
        }
        else
        {
            defaultNode = CreateStandardActionNode(ActionDetail.Default.ToString());
            result.Messages.Add($"[{skinName}] 已创建标准 Default 模板。");
        }

        EnsureStandardActionShape(defaultNode);
        clothInfo.Add(defaultNode);
        result.HasChanges = true;

        if (!string.IsNullOrWhiteSpace(sourceActionName) &&
            !string.Equals(sourceActionName, ActionDetail.Default.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            CopyActionImages(modInfoRoot, sourceActionName, ActionDetail.Default.ToString(), result);
        }

        return result;
    }

    public static XElement CreateStandardActionNode(string actionName)
    {
        var node = new XElement(actionName);
        EnsureStandardActionShape(node);
        return node;
    }

    private static IEnumerable<XElement> GetActionNodes(XElement clothInfo) =>
        clothInfo.Elements().Where(node => !NonActionTags.Contains(node.Name.LocalName));

    private static bool IsAction(XElement node, ActionDetail actionDetail) =>
        string.Equals(node.Name.LocalName, actionDetail.ToString(), StringComparison.OrdinalIgnoreCase);

    private static XElement? FindActionNode(XElement clothInfo, string actionName) =>
        GetActionNodes(clothInfo).FirstOrDefault(n =>
            string.Equals(n.Name.LocalName, actionName, StringComparison.OrdinalIgnoreCase));

    private static XElement? FindFirstCompatibleSource(XElement clothInfo, out string? actionName)
    {
        foreach (var action in PresetActions)
        {
            if (action == ActionDetail.Default) continue;
            var node = FindActionNode(clothInfo, action.ToString());
            if (node != null)
            {
                actionName = node.Name.LocalName;
                return node;
            }
        }

        actionName = null;
        return null;
    }

    private static void EnsureStandardActionShape(XElement actionNode)
    {
        var directionNode = actionNode.Element("Direction");
        if (directionNode == null)
        {
            actionNode.AddFirst(new XElement("Direction", "Front"));
        }
        else if (string.IsNullOrWhiteSpace(directionNode.Value))
        {
            directionNode.Value = "Front";
        }

        EnsureAttr(actionNode, "size_x", "512");
        EnsureAttr(actionNode, "size_y", "512");
        EnsureAttr(actionNode, "quality", "50");

        var pivotNode = actionNode.Element("Pivot");
        if (pivotNode == null)
        {
            pivotNode = new XElement("Pivot");
            actionNode.Add(pivotNode);
        }

        EnsureAttr(pivotNode, "pivot_x", "0");
        EnsureAttr(pivotNode, "pivot_y", "0");

        var headNode = actionNode.Element("Head");
        if (headNode == null)
        {
            headNode = new XElement("Head");
            actionNode.Add(headNode);
        }

        EnsureAttr(headNode, "head_x", "0");
        EnsureAttr(headNode, "head_y", "0");
        EnsureAttr(headNode, "head_enable", "true");
    }

    private static void EnsureAttr(XElement node, string attrName, string defaultValue)
    {
        if (node.Attribute(attrName) == null)
            node.SetAttributeValue(attrName, defaultValue);
    }

    private static void CopyActionImages(XElement modInfoRoot, string sourceAction, string targetAction, SkinFixResult result)
    {
        try
        {
            var modInfoPath = modInfoRoot.Annotation<FilePathAnnotation>()?.Path;
            if (string.IsNullOrWhiteSpace(modInfoPath)) return;

            var skinDir = Path.GetDirectoryName(modInfoPath);
            if (string.IsNullOrWhiteSpace(skinDir)) return;

            var clothCustomDir = Path.Combine(skinDir, "ClothCustom");
            if (!Directory.Exists(clothCustomDir)) return;

            foreach (var suffix in CopyImageSuffixes)
            {
                var sourcePath = Path.Combine(clothCustomDir, $"{sourceAction}{suffix}.png");
                if (!File.Exists(sourcePath)) continue;

                var targetPath = Path.Combine(clothCustomDir, $"{targetAction}{suffix}.png");
                File.Copy(sourcePath, targetPath, true);
                result.HasChanges = true;
                result.Messages.Add(
                    $"[{result.SkinName}] 图片迁移: {Path.GetFileName(sourcePath)} -> {Path.GetFileName(targetPath)}");
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"[{result.SkinName}] 迁移动作图片失败: {ex.Message}");
        }
    }

    private static string ResolveSkinName(XElement modInfoRoot, XElement? clothInfo)
    {
        var fromXml = clothInfo?.Element("Name")?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(fromXml))
            return fromXml;

        var docPath = modInfoRoot.Annotation<FilePathAnnotation>()?.Path;
        if (!string.IsNullOrWhiteSpace(docPath))
        {
            var folder = Path.GetDirectoryName(docPath);
            if (!string.IsNullOrWhiteSpace(folder))
                return Path.GetFileName(folder);
        }

        return "UnknownSkin";
    }
}
