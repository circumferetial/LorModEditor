using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Synthesis.Core.Attributes;
using Synthesis.Core.Tools;

namespace Synthesis.Core.Abstraction;

public abstract class XWrapper(XElement element) : INotifyPropertyChanged
{
    public XElement Element { get; protected set; } = element;

    // ... GlobalId, IsVanilla 保持不变 ...
    [NoAutoInit]
    public LorId GlobalId
    {
        get
        {
            var id = Element.Attribute("ID")?.Value ?? Element.Attribute("id")?.Value ?? Element.Value;
            var packageId = "Unknown";
            if (Element.Document?.Root != null)
            {
                foreach (var annotation in Element.Document.Root.Annotations<string>())
                {
                    if (!annotation.StartsWith("PID:")) continue;
                    packageId = annotation[4..];
                    break;
                }
            }
            return new LorId(packageId, id);
        }
    }

    [NoAutoInit] public bool IsVanilla => GlobalId.IsVanilla;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value)) return false;
        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected static double GetDouble(XElement? parent, string name, double def = 0) =>
        (parent?.Element(name)?.Value).ToDouble(def);

    protected void SetDouble(XElement? parent, string name, double val) =>
        SetElementValue(parent, name, val.Format());

    protected static double GetDoubleAttr(XElement? el, string name, double def = 0) =>
        GetAttr(el, name).ToDouble(def);

    protected void SetDoubleAttr(XElement? el, string name, double val) =>
        SetAttr(el, name, val.Format());

    public void InitDefaults()
    {
        if (IsVanilla) return;

        // 获取当前类的所有公共属性
        var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            // 跳过只读属性、索引器等
            if (!prop.CanRead || !prop.CanWrite) continue;
            if (prop.GetCustomAttribute<NoAutoInitAttribute>() != null) continue;

            if (prop.PropertyType.IsGenericType) continue;// 跳过 List/Collection

            try
            {
                // 1. 读取属性当前的值 (Getter)
                // 此时 XML 里可能没有节点，Getter 会返回默认值 (比如 HP=10)
                var value = prop.GetValue(this);

                // 2. 写入属性 (Setter)
                // 这会触发 SetElementValue / SetAttr，把默认值强制写入 XML
                prop.SetValue(this, value);
            }
            catch
            {
                /* 忽略反射错误 */
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    // --- String (Attribute) ---
    protected static string GetAttr(XElement? el, string name, string def = "") => el?.Attribute(name)?.Value ?? def;

    protected void SetAttr(XElement? el, string name, string? val)
    {
        if (el == null || IsVanilla) return;

        // 【修改】即使是空字符串，也应该写入属性 (如 Script="")，除非是 null
        // 如果你确实希望 "空" = "删除属性"，保留原逻辑；
        // 但对于数值类型，建议总是写入。

        el.SetAttributeValue(name, val);

        OnPropertyChanged(null);
    }

    // --- String (Element) ---
    protected static string GetElementValue(XElement? parent, string name, string def = "") =>
        parent?.Element(name)?.Value ?? def;

    protected void SetElementValue(XElement? parent, string name, string? val)
    {
        if (parent == null || IsVanilla) return;
        var node = parent.Element(name);
        if (node == null)
        {
            node = new XElement(name);
            parent.Add(node);
        }

        // 【修改】强制写入值，不删除节点
        node.Value = val ?? "";

        OnPropertyChanged(null);
    }

    // =========================================================
    // 【核心修复】强类型读写 (区分 Element 和 Attribute)
    // =========================================================

    // --- Int ---
    // 读：取不到返回默认值 (用于 UI 显示)
    // 写：强制写入字符串 (用于 XML 保存)

    protected static int GetInt(XElement? parent, string name, int def = 0) =>
        (parent?.Element(name)?.Value).ToInt(def);

    // 【关键】SetInt 调用 SetElementValue，现在会强制生成 <HP>10</HP>，而不会因为它是默认值就跳过
    protected void SetInt(XElement? parent, string name, int val) =>
        SetElementValue(parent, name, val.Format());

    protected static int GetIntAttr(XElement? el, string name, int def = 0) => GetAttr(el, name).ToInt(def);
    protected void SetIntAttr(XElement? el, string name, int val) => SetAttr(el, name, val.Format());

    // --- Bool ---
    protected static bool GetBool(XElement? parent, string name, bool def = false) =>
        (parent?.Element(name)?.Value).ToBool(def);

    protected void SetBool(XElement? parent, string name, bool val) => SetElementValue(parent, name, val.Format());

    protected static bool GetBoolAttr(XElement? el, string name, bool def = false) => GetAttr(el, name).ToBool(def);
    protected void SetBoolAttr(XElement? el, string name, bool val) => SetAttr(el, name, val.Format());

    // --- Enum ---
    protected static T GetEnum<T>(XElement? parent, string name, T def) where T : struct, Enum =>
        (parent?.Element(name)?.Value).ToEnum(def);

    protected void SetEnum<T>(XElement? parent, string name, T val) where T : struct =>
        SetElementValue(parent, name, val.Format());

    protected static T GetEnumAttr<T>(XElement el, string name, T def) where T : struct, Enum =>
        GetAttr(el, name).ToEnum(def);

    protected void SetEnumAttr<T>(XElement el, string name, T val) where T : struct => SetAttr(el, name, val.Format());

    // --- LorId ---
    // ... (保持你之前的 LorId 读写逻辑不变) ...
    protected LorId GetLorId(XElement? parent, string tagName)
    {
        var node = parent?.Element(tagName);
        if (node == null) return default;
        return LorId.ParseXmlReference(node, GlobalId.PackageId);
    }

    protected void SetLorId(XElement? parent, string tagName, LorId? value)
    {
        if (parent == null || IsVanilla || value == null) return;

        var node = parent.Element(tagName);
        if (node == null)
        {
            // 如果值为空，且节点不存在，就不创建了
            if (string.IsNullOrEmpty(value.Value.ItemId)) return;
            node = new XElement(tagName);
            parent.Add(node);
        }

        node.Value = value.Value.ItemId;

        if (value.Value.PackageId != GlobalId.PackageId && !string.IsNullOrEmpty(value.Value.PackageId))
        {
            node.SetAttributeValue("Pid", value.Value.PackageId);
        }
        else
        {
            node.Attribute("Pid")?.Remove();
        }
        OnPropertyChanged(null);
    }
}