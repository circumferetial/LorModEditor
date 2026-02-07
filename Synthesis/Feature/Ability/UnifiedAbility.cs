using System.Xml.Linq;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Attributes;

namespace Synthesis.Feature.Ability;

public class UnifiedAbility : XWrapper
{
    private static readonly char[] separator = ['\r', '\n'];

    public UnifiedAbility(XElement element) : base(element)
    {
        InitDefaults();
    }

    public string Id
    {
        get => GetAttr(Element, "ID");
        set
        {
            SetAttr(Element, "ID", value);
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    public string Desc
    {
        get
        {
            // 读取：合并所有 Desc 标签，用换行符连接
            var lines = Element.Elements("Desc").Select(e => e.Value);
            return string.Join(Environment.NewLine, lines);
        }
        set
        {
            if (IsVanilla) return;

            // 写入：先清空，再分割字符串，创建多个 Desc 标签
            Element.Elements("Desc").Remove();

            if (!string.IsNullOrEmpty(value))
            {
                var lines = value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    Element.Add(new XElement("Desc", line));
                }
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayName));
        }
    }

    // 列表显示：ID + 描述前20字
    [NoAutoInit]
    public string DisplayName => $"{Id} - {string.Concat(Desc.Replace("\r", "").Replace("\n", " ").Take(20))}...";

    public void DeleteXml()
    {
        if (IsVanilla) return;
        Element.Remove();
    }
}