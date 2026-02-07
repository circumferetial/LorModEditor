using System.IO;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Tools; // 确保引用了 FilePathAnnotation

namespace Synthesis.Feature.SkinEditor;

public class SkinRepository : BaseRepository<SkinEntry>
{
    public override void LoadResources(string projectRoot, string language, string modId)
    {

        // 路径: (ProjectRoot)/Resource/CharacterSkin
        var skinRoot = Path.Combine(projectRoot, "Resource", "CharacterSkin");

        if (!Directory.Exists(skinRoot)) return;

        // 遍历所有皮肤文件夹
        var directories = Directory.GetDirectories(skinRoot);
        foreach (var dir in directories)
        {
            var modInfoPath = Path.Combine(dir, "ModInfo.xml");
            if (File.Exists(modInfoPath))
            {
                try
                {
                    var doc = XDocument.Load(modInfoPath);
                    if (doc.Root != null)
                    {
                        // 标记并保存 (这是为了 BaseRepository 的保存功能能正常工作)
                        doc.Root.AddAnnotation("PID:" + modId);
                        doc.Root.AddAnnotation(new FilePathAnnotation(modInfoPath));
                        AddDataDoc(doc); // 加入到待保存列表

                        // 创建 SkinEntry 并添加到界面列表
                        var skin = new SkinEntry(doc.Root, dir);
                        Items.Add(skin);
                    }
                }
                catch
                {
                    // 忽略加载失败的文件
                }
            }
        }
    }

    public override void EnsureDefaults(string projectRoot, string language, string modId)
    {
        Directory.CreateDirectory(Path.Combine(projectRoot, "Resource", "CharacterSkin"));
    }

    public override void Load()
    {
        // BaseRepository 要求的抽象方法，这里留空即可，逻辑在 LoadResources 里
    }
}