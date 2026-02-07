using System.IO;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;

namespace Synthesis.Feature.SkinEditor;

// 代表一个完整的皮肤 (对应一个文件夹)
public class SkinEntry(XElement element, string folderPath) : XWrapper(element)
{
    public string FolderPath { get; } = folderPath;

    // 皮肤文件夹名 (作为唯一标识)
    public string FolderName => Path.GetFileName(FolderPath);

    // XML 里的显示名 (<Name>...</Name>)
    public string DisplayName
    {
        get => GetElementValue(Element.Element("ClothInfo"), "Name", FolderName);
        set => SetElementValue(Element.Element("ClothInfo"), "Name", value);
    }

    // 获取该皮肤下所有的动作
    public List<CharacterMotionData> GetAllMotions()
    {
        var list = new List<CharacterMotionData>();
        var clothInfo = Element.Element("ClothInfo");
        if (clothInfo == null) return list;

        foreach (var node in clothInfo.Elements())
        {
            // 排除非动作节点
            if (node.Name.LocalName == "Name" || 
                node.Name.LocalName == "SoundList" || 
                node.Name.LocalName == "AtkEffectPivotInfo" ||
                node.Name.LocalName == "SpecialMotionPivotInfo" ||
                node.Name.LocalName.Contains("Info")) continue;

            list.Add(new CharacterMotionData(node));
        }
        return list;
    }

    // 获取图片路径: .../ClothCustom/动作名.png
    public string GetImagePath(string motionName)
    {
        return Path.Combine(FolderPath, "ClothCustom", $"{motionName}.png");
    }
}