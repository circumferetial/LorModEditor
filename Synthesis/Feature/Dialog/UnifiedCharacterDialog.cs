using System.Xml.Linq;
using Synthesis.Core.Abstraction;

namespace Synthesis.Feature.Dialog;

// 极简包装，只为了读取 ID 判重
public class UnifiedCharacterDialog : XWrapper
{
    public UnifiedCharacterDialog(XElement element) : base(element)
    {
        InitDefaults();
    }

    public string CharacterId => GetAttr(Element, "ID");
}