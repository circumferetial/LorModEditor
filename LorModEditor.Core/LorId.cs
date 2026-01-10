using System.Xml.Linq;

namespace LorModEditor.Core;

public struct LorId(string packageId, string itemId) : IEquatable<LorId>
{
    public string PackageId { get; set; } = packageId;// Mod ID (Workshop ID)
    public string ItemId { get; set; } = itemId;// 具体的 ID (如 "10001")

    // 构造函数
    public const string Vanilla = "@origin";

    // 核心逻辑：判断是否为原版
    // BaseMod 通常把原版内容的 Pid 视为  "@origin"
    public readonly bool IsVanilla => PackageId.Equals(Vanilla, StringComparison.OrdinalIgnoreCase);

    // 显示用的字符串
    public override readonly string ToString() => IsVanilla ? $"[原版] {ItemId}" : $"[{PackageId}] {ItemId}";

    // --- 从 XML 引用中解析 (用于读取 <Passive Pid="...">) ---
    public static LorId ParseXmlReference(XElement? element, string defaultPackageId)
    {
        if (element == null) return default;

        var pid = element.Attribute("Pid")?.Value;
        var id = element.Value;// 或者是 element.Attribute("ID")?.Value，视情况而定

        // 如果 XML 里没写 Pid，就默认为当前 Mod (defaultPackageId)
        if (string.IsNullOrEmpty(pid)) pid = defaultPackageId;

        return new LorId(pid, id);
    }

    // --- 比较逻辑 (用于字典 Key) ---
    public readonly bool Equals(LorId other) => PackageId == other.PackageId && ItemId == other.ItemId;

    public override readonly bool Equals(object? obj) => obj is LorId other && Equals(other);

    public override readonly int GetHashCode() => HashCode.Combine(PackageId, ItemId);

    public static bool operator ==(LorId left, LorId right) => left.Equals(right);

    public static bool operator !=(LorId left, LorId right) => !left.Equals(right);
}