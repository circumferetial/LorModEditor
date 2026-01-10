using System.Xml.Linq;

namespace LorModEditor.Core.Wrappers;

// 极简包装，只为了读取 ID 判重
public class UnifiedCharacterDialog : XWrapper
{
    public UnifiedCharacterDialog(XElement element) : base(element)
    {
        InitDefaults();
    }

    public string CharacterId => GetAttr(Element, "ID");
}