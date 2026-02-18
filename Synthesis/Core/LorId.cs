using System.Xml.Linq;

namespace Synthesis.Core;

public struct LorId(string packageId, string itemId) : IEquatable<LorId>
{
    public const string Vanilla = "@origin";

    public string PackageId { get; set; } = packageId;

    public string ItemId { get; set; } = itemId;

    public readonly bool IsVanilla => PackageId.Equals("@origin", StringComparison.OrdinalIgnoreCase);

    public override readonly string ToString()
    {
        if (!IsVanilla)
        {
            return "[" + PackageId + "] " + ItemId;
        }
        return "[原版] " + ItemId;
    }

    public static LorId ParseXmlReference(XElement? element, string defaultPackageId)
    {
        if (element == null)
        {
            return default;
        }
        var text = element.Attribute("Pid")?.Value;
        var value = element.Value;
        if (string.IsNullOrEmpty(text))
        {
            text = defaultPackageId;
        }
        return new LorId(text, value);
    }

    public readonly bool Equals(LorId other)
    {
        if (PackageId == other.PackageId)
        {
            return ItemId == other.ItemId;
        }
        return false;
    }

    public override readonly bool Equals(object? obj)
    {
        if (obj is LorId other)
        {
            return Equals(other);
        }
        return false;
    }

    public override readonly int GetHashCode() => HashCode.Combine(PackageId, ItemId);

    public static bool operator ==(LorId left, LorId right) => left.Equals(right);

    public static bool operator !=(LorId left, LorId right) => !left.Equals(right);
}
