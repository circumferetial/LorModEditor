using System.Xml.Linq;

namespace Synthesis.Core.Extensions;

public static class XmlExtensions
{
    public static bool IsVanilla(this XDocument? doc)
    {
        if (doc?.Root != null)
        {
            return doc.Root.IsVanilla();
        }
        return false;
    }

    public static bool IsVanilla(this XElement? element)
    {
        if (element == null)
        {
            return false;
        }
        return (element.Document?.Root ?? element).Annotations<string>()
            .Any(a => a.Equals("PID:@origin", StringComparison.OrdinalIgnoreCase));
    }

    public static string GetPackageId(this XDocument? doc)
    {
        if (doc?.Root == null)
        {
            return "Unknown";
        }
        foreach (var item in doc.Root.Annotations<string>())
        {
            if (item.StartsWith("PID:"))
            {
                var text = item;
                return text.Substring(4, text.Length - 4);
            }
        }
        return "Unknown";
    }
}
