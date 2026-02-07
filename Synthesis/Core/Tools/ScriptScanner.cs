using System.Collections.ObjectModel;
using System.IO;
using Mono.Cecil;

// 需安装 NuGet 包: Mono.Cecil

namespace Synthesis.Core.Tools;

public class ScriptScanner
{
    public ObservableCollection<string> CardScripts { get; } = [];
    public ObservableCollection<string> DiceScripts { get; } = [];
    public ObservableCollection<string> PassiveScripts { get; } = [];

    public void Scan(string modRootPath)
    {
        CardScripts.Clear();
        DiceScripts.Clear();
        PassiveScripts.Clear();

        if (!Directory.Exists(modRootPath)) return;

        // 1. 扫描根目录下的 DLL (和 StaticInfo 平行)
        var dlls = Directory.GetFiles(modRootPath, "*.dll");

        foreach (var dllPath in dlls)
        {
            ReadDll(dllPath);
        }
    }

    private void ReadDll(string path)
    {
        try
        {
            // ReadSymbols: false 避免找不到 pdb 文件报错
            using var assembly = AssemblyDefinition.ReadAssembly(path, new ReaderParameters { ReadSymbols = false });
            foreach (var type in assembly.Modules.SelectMany(module => module.Types))
            {
                if (type.IsAbstract || type.BaseType == null) continue;

                var baseName = type.BaseType.Name;
                var className = type.Name;

                // 2. 根据基类判断，并切除前缀

                // 卡牌脚本
                if (baseName.Contains("DiceCardSelfAbility"))
                {
                    if (className.StartsWith("DiceCardSelfAbility_"))
                        className = className["DiceCardSelfAbility_".Length..];
                    AddUnique(CardScripts, className);
                }
                // 骰子脚本
                else if (baseName.Contains("DiceCardAbility"))
                {
                    if (className.StartsWith("DiceCardAbility_"))
                        className = className["DiceCardAbility_".Length..];
                    AddUnique(DiceScripts, className);
                }
                // 被动脚本
                else if (baseName.Contains("PassiveAbilityBase"))
                {
                    if (className.StartsWith("PassiveAbility_"))
                        className = className["PassiveAbility_".Length..];
                    AddUnique(PassiveScripts, className);
                }
            }
        }
        catch
        {
            // 忽略非托管DLL或损坏的文件 
        }
    }

    private static void AddUnique(ObservableCollection<string> list, string item)
    {
        if (!list.Contains(item)) list.Add(item);
    }
}