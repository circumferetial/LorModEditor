using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Synthesis.Core.Attributes;
using Synthesis.Core.Tools;

namespace Synthesis.Core.Abstraction;

public abstract class XWrapper(XElement element) : INotifyPropertyChanged
{
    private readonly string _packageId = ResolvePackageId(element);

    public XElement Element { get; protected set; } = element;

    [NoAutoInit]
    public LorId GlobalId
    {
        get
        {
            var itemId = Element.Attribute("ID")?.Value ?? Element.Attribute("id")?.Value ?? Element.Value;
            return new LorId(_packageId, itemId);
        }
    }

    [NoAutoInit] public bool IsVanilla => _packageId.Equals(LorId.Vanilla, StringComparison.OrdinalIgnoreCase);

    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
        {
            return false;
        }
        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected static double GetDouble(XElement? parent, string name, double def = 0.0) =>
        (parent?.Element(name)?.Value).ToDouble(def);

    protected void SetDouble(XElement? parent, string name, double val)
    {
        SetElementValue(parent, name, val.Format());
    }

    protected static double GetDoubleAttr(XElement? el, string name, double def = 0.0) =>
        GetAttr(el, name).ToDouble(def);

    protected void SetDoubleAttr(XElement? el, string name, double val)
    {
        SetAttr(el, name, val.Format());
    }

    public void InitDefaults()
    {
        if (IsVanilla)
        {
            return;
        }
        var properties = GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var propertyInfo in properties)
        {
            if (propertyInfo.CanRead && propertyInfo.CanWrite &&
                propertyInfo.GetCustomAttribute<NoAutoInitAttribute>() == null &&
                !propertyInfo.PropertyType.IsGenericType)
            {
                try
                {
                    var value = propertyInfo.GetValue(this);
                    propertyInfo.SetValue(this, value);
                }
                catch
                {
                }
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    protected static string GetAttr(XElement? el, string name, string def = "") => el?.Attribute(name)?.Value ?? def;

    protected void SetAttr(XElement? el, string name, string? val)
    {
        if (el != null && !IsVanilla)
        {
            el.SetAttributeValue(name, val);
            OnPropertyChanged(null);
        }
    }

    protected static string GetElementValue(XElement? parent, string name, string def = "") =>
        parent?.Element(name)?.Value ?? def;

    protected void SetElementValue(XElement? parent, string name, string? val)
    {
        if (parent != null && !IsVanilla)
        {
            var xElement = parent.Element(name);
            if (xElement == null)
            {
                xElement = new XElement(name);
                parent.Add(xElement);
            }
            xElement.Value = val ?? "";
            OnPropertyChanged(null);
        }
    }

    protected static int GetInt(XElement? parent, string name, int def = 0) =>
        (parent?.Element(name)?.Value).ToInt(def);

    protected void SetInt(XElement? parent, string name, int val)
    {
        SetElementValue(parent, name, val.Format());
    }

    protected static int GetIntAttr(XElement? el, string name, int def = 0) => GetAttr(el, name).ToInt(def);

    protected void SetIntAttr(XElement? el, string name, int val)
    {
        SetAttr(el, name, val.Format());
    }

    protected static bool GetBool(XElement? parent, string name, bool def = false) =>
        (parent?.Element(name)?.Value).ToBool(def);

    protected void SetBool(XElement? parent, string name, bool val)
    {
        SetElementValue(parent, name, val.Format());
    }

    protected static bool GetBoolAttr(XElement? el, string name, bool def = false) => GetAttr(el, name).ToBool(def);

    protected void SetBoolAttr(XElement? el, string name, bool val)
    {
        SetAttr(el, name, val.Format());
    }

    protected static T GetEnum<T>(XElement? parent, string name, T def) where T : struct, Enum =>
        (parent?.Element(name)?.Value).ToEnum(def);

    protected void SetEnum<T>(XElement? parent, string name, T val) where T : struct
    {
        SetElementValue(parent, name, val.Format());
    }

    protected static T GetEnumAttr<T>(XElement el, string name, T def) where T : struct, Enum =>
        GetAttr(el, name).ToEnum(def);

    protected void SetEnumAttr<T>(XElement el, string name, T val) where T : struct
    {
        SetAttr(el, name, val.Format());
    }

    protected LorId GetLorId(XElement? parent, string tagName)
    {
        var xElement = parent?.Element(tagName);
        if (xElement == null)
        {
            return default;
        }
        return LorId.ParseXmlReference(xElement, GlobalId.PackageId);
    }

    protected void SetLorId(XElement? parent, string tagName, LorId? value)
    {
        if (parent == null || IsVanilla || !value.HasValue)
        {
            return;
        }
        var xElement = parent.Element(tagName);
        if (xElement == null)
        {
            if (string.IsNullOrEmpty(value.Value.ItemId))
            {
                return;
            }
            xElement = new XElement(tagName);
            parent.Add(xElement);
        }
        xElement.Value = value.Value.ItemId;
        if (value.Value.PackageId != GlobalId.PackageId && !string.IsNullOrEmpty(value.Value.PackageId))
        {
            xElement.SetAttributeValue("Pid", value.Value.PackageId);
        }
        else
        {
            xElement.Attribute("Pid")?.Remove();
        }
        OnPropertyChanged(null);
    }

    private static string ResolvePackageId(XElement sourceElement)
    {
        if (sourceElement.Document?.Root != null)
        {
            foreach (var item in sourceElement.Document.Root.Annotations<string>())
            {
                if (item.StartsWith("PID:"))
                {
                    return item.Substring(4);
                }
            }
        }

        return "Unknown";
    }
}
