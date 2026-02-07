using System.IO;
using System.Xml.Linq;
using Synthesis.Core;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Extensions;
using Synthesis.Core.Tools;

namespace Synthesis.Feature.OldSkinEditor;

public class SkinRepository : BaseRepository<UnifiedSkin>
{
    // 白名单：游戏原版支持的动作
    // 我们把 Enum 转成字符串 Set，方便快速查找
    private static readonly HashSet<string> _standardActions =
        [..GlobalValues.SkinActions.Select(x => x.ToString())];

    public override void LoadResources(string root, string lang, string modId)
    {
        var skinRoot = Path.Combine(root, "Resource", "CharacterSkin");
        if (Directory.Exists(skinRoot))
        {
            foreach (var dir in Directory.GetDirectories(skinRoot))
            {
                var modInfoPath = Path.Combine(dir, "ModInfo.xml");
                var extraPath = Path.Combine(dir, "ModInfo_Extra.xml");// 自定义动作文件

                if (File.Exists(modInfoPath))
                {
                    try
                    {
                        var doc = XDocument.Load(modInfoPath);
                        if (doc.Root?.Name.LocalName == "ModInfo")
                        {
                            // 【核心逻辑 1：读取时合并】
                            // 如果存在 Extra 文件，把它里面的动作“偷”过来，塞进内存里的 doc
                            if (File.Exists(extraPath))
                            {
                                MergeExtraFile(doc, extraPath);
                            }

                            doc.Root.AddAnnotation("PID:" + modId);
                            doc.Root.AddAnnotation(new FilePathAnnotation(modInfoPath));
                            AddDataDoc(doc);
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }
    }

    private void MergeExtraFile(XDocument mainDoc, string extraPath)
    {
        try
        {
            var extraDoc = XDocument.Load(extraPath);
            var mainCloth = mainDoc.Root?.Element("ClothInfo");
            var extraCloth = extraDoc.Root?.Element("ClothInfo");

            if (mainCloth != null && extraCloth != null)
            {
                foreach (var element in extraCloth.Elements())
                {
                    // 跳过 Name 节点，只合并动作
                    if (element.Name.LocalName != "Name")
                    {
                        // 必须 Clone 一份，因为一个节点不能属于两个文档
                        mainCloth.Add(new XElement(element));
                    }
                }
            }
        }
        catch
        {
            /* 忽略合并错误，保证主文件能读 */
        }
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        // Skin 不强制创建默认文件
    }

    public override void Load()
    {
        foreach (var doc in _dataDocs)
        {
            if (doc.Root?.Name.LocalName == "ModInfo")
            {
                Items.Add(new UnifiedSkin(doc.Root));
            }
        }
    }

    // 创建新皮肤
    public UnifiedSkin? Create(string projectRoot, string skinName, string modId)
    {
        var skinDir = Path.Combine(projectRoot, "Resource", "CharacterSkin", skinName);
        if (!Directory.Exists(skinDir)) Directory.CreateDirectory(skinDir);

        var xmlPath = Path.Combine(skinDir, "ModInfo.xml");
        if (!File.Exists(xmlPath))
        {
            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("ModInfo"));
            var clothInfo = new XElement("ClothInfo");
            clothInfo.Add(new XElement("Name", skinName));
            doc.Root!.Add(clothInfo);

            doc.Save(xmlPath);
            doc.Root.AddAnnotation("PID:" + modId);
            doc.Root.AddAnnotation(new FilePathAnnotation(xmlPath));

            AddDataDoc(doc);
            var unifiedSkin = new UnifiedSkin(doc.Root);
            Items.Add(unifiedSkin);
            return unifiedSkin;
        }
        return null;
    }

    public override void Delete(UnifiedSkin item)
    {
        // 简单处理：清空内容，实际删除文件比较危险
        item.Element.Element("ClothInfo")?.Remove();
        base.Delete(item);
    }

    // =========================================================
    // 【核心逻辑 2：保存时拆分】
    // =========================================================
    public override void SaveFiles(string currentModId)
    {
        foreach (var doc in _dataDocs)
        {
            // 只保存当前 Mod 的文件
            if (doc.GetPackageId() != currentModId) continue;

            var path = doc.Root?.Annotation<FilePathAnnotation>()?.Path;
            if (string.IsNullOrEmpty(path)) continue;

            // 准备拆分
            var dir = Path.GetDirectoryName(path)!;
            var extraPath = Path.Combine(dir, "ModInfo_Extra.xml");

            // 1. 克隆两份文档 (深拷贝)
            var standardDoc = new XDocument(doc);
            var extraDoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("ModInfo"));
            extraDoc.Root!.Add(new XElement("ClothInfo"));// 准备好结构

            var stdCloth = standardDoc.Root?.Element("ClothInfo");
            var extraCloth = extraDoc.Root?.Element("ClothInfo");

            if (stdCloth != null && extraCloth != null)
            {
                // 复制 Name 到 Extra (可选，方便辨认)
                var nameVal = stdCloth.Element("Name")?.Value;
                if (nameVal != null) extraCloth.Add(new XElement("Name", nameVal));

                // 2. 遍历所有动作，进行分类
                // 我们遍历 standardDoc 的副本，把不该存在的删掉，移到 extraDoc
                var allActions = stdCloth.Elements().ToList();// ToList 避免修改集合时报错

                foreach (var el in allActions)
                {
                    var tagName = el.Name.LocalName;
                    if (tagName == "Name") continue;// Name 保留

                    // 检查是否为标准动作
                    if (_standardActions.Contains(tagName)) continue;
                    // 是自定义动作 -> 移到 extraDoc
                    extraCloth.Add(new XElement(el));// 复制到 Extra
                    el.Remove();// 从 Standard 中删除
                }

                // 3. 保存文件
                standardDoc.Save(path);// 保存纯净版 ModInfo.xml

                // 如果有自定义动作，保存 ModInfo_Extra.xml
                // (排除掉只有 <Name> 的情况)
                if (extraCloth.Elements().Count() > 1)
                {
                    extraDoc.Save(extraPath);
                }
                else
                {
                    // 如果没有自定义动作，且文件存在，则删除它 (保持文件夹干净)
                    if (File.Exists(extraPath)) File.Delete(extraPath);
                }
            }
        }
    }
}