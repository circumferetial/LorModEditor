using System.Xml.Linq;

namespace Synthesis.Core.Extensions;

public static class XmlExtensions
{
    /// <summary>
    ///     判断 XML 文档是否属于原版数据
    /// </summary>
    public static bool IsVanilla(this XDocument? doc) =>
        // 如果文档为空，或者没有 Root，肯定不是原版（或者是无效的）
        doc?.Root != null && doc.Root.IsVanilla();

    /// <summary>
    ///     判断 XML 元素是否属于原版数据
    /// </summary>
    public static bool IsVanilla(this XElement? element)
    {
        if (element == null) return false;

        // 获取文档根节点 (因为 Annotation 是打在 Root 上的)
        // 如果 element 本身就是 Root，或者它没有 Document，就检查它自己
        var target = element.Document?.Root ?? element;

        var annotations = target.Annotations<string>();

        // 检查是否存在 PID 标记
        return annotations.Any(a =>
            a.Equals("PID:@origin", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     获取文档所属的 PackageId (Mod ID)
    /// </summary>
    public static string GetPackageId(this XDocument? doc)
    {
        if (doc?.Root == null) return "Unknown";

        foreach (var ann in doc.Root.Annotations<string>())
        {
            if (ann.StartsWith("PID:"))
            {
                return ann[4..];
            }
        }
        return "Unknown";
    }
}