using System.Xml.Linq;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Attributes;

namespace Synthesis.Feature.Ability;

public class UnifiedAbility : XWrapper
{
    private static readonly char[] separator = ['\r', '\n'];

    public UnifiedAbility(XElement element)
        : base(element)
    {
        InitDefaults();
    }

    public string Id
    {
        get => GetAttr(Element, "ID");
        set
        {
            SetAttr(Element, "ID", value);
            OnPropertyChanged("DisplayName");
        }
    }

    public string Desc
    {
        get
        {
            IEnumerable<string> values = from e in Element.Elements("Desc")
                select e.Value;
            return string.Join(Environment.NewLine, values);
        }
        set
        {
            if (IsVanilla)
            {
                return;
            }
            Element.Elements("Desc").Remove();
            if (!string.IsNullOrEmpty(value))
            {
                var array = value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                foreach (var content in array)
                {
                    Element.Add(new XElement("Desc", content));
                }
            }
            OnPropertyChanged();
            OnPropertyChanged("DisplayName");
        }
    }

    [NoAutoInit]
    public string DisplayName =>
        string.Concat(Id, " - ", string.Concat(Desc.Replace("\r", "").Replace("\n", " ").Take(20)), "...");

    public void DeleteXml()
    {
        if (!IsVanilla)
        {
            Element.Remove();
        }
    }
}
