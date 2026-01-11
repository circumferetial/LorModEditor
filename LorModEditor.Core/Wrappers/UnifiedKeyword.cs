using System.Xml.Linq;
using JetBrains.Annotations;
using LorModEditor.Core.Attributes;

namespace LorModEditor.Core.Wrappers;

public class UnifiedKeyword : XWrapper
{
    public UnifiedKeyword(XElement element, XElement? parent) : base(element)
    {
        Parent = parent;
        InitDefaults();
    }

    [UsedImplicitly] public XElement? Parent { get; }

    public string Id
    {
        get => GetAttr(Element, "ID");
        set
        {
            SetAttr(Element, "ID", value);
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    public string Name
    {
        get => GetElementValue(Element, "Name", "New Keyword");
        set
        {
            SetElementValue(Element, "Name", value);
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    public string Desc
    {
        get => GetElementValue(Element, "Desc", "Desc...");
        set => SetElementValue(Element, "Desc", value);
    }

    [NoAutoInit] public string DisplayName => $"{Id} - {Name}";

    // 其实对于纯文本 XML，Wrapper 很少需要自己创建自己 (因为 Create 时已经创建好了)
    // 但如果我们需要支持“复制”或者特殊操作，保留这个 parent 引用是有用的。
    // Keyword 没有类似 EnsureTextNode 的复杂逻辑，因为它本身就是 TextNode。

    public void DeleteXml()
    {
        if (IsVanilla) return;
        Element.Remove();
    }
}