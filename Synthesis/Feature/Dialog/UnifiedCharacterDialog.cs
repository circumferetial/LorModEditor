using System.Xml.Linq;
using Synthesis.Core.Abstraction;

namespace Synthesis.Feature.Dialog;

public class UnifiedCharacterDialog : XWrapper
{
    public UnifiedCharacterDialog(XElement element)
        : base(element)
    {
        InitDefaults();
    }

    public string CharacterId => GetAttr(Element, "ID");
}
