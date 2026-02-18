using System.IO;
using Mono.Cecil;

namespace Synthesis.Core.Tools;

public class ScriptScanner
{
    public List<string> CardScripts { get; } = [];

    public List<string> DiceScripts { get; } = [];

    public List<string> PassiveScripts { get; } = [];

    public void Scan(string modRootPath)
    {
        CardScripts.Clear();
        DiceScripts.Clear();
        PassiveScripts.Clear();
        if (Directory.Exists(modRootPath))
        {
            var files = Directory.GetFiles(modRootPath, "*.dll");
            foreach (var path in files)
            {
                ReadDll(path);
            }
        }
    }

    private void ReadDll(string path)
    {
        try
        {
            using var assemblyDefinition = AssemblyDefinition.ReadAssembly(path, new ReaderParameters
            {
                ReadSymbols = false
            });
            foreach (var item in assemblyDefinition.Modules.SelectMany(module => module.Types))
            {
                if (item.IsAbstract || item.BaseType == null)
                {
                    continue;
                }
                var name = item.BaseType.Name;
                var text = item.Name;
                if (name.Contains("DiceCardSelfAbility"))
                {
                    if (text.StartsWith("DiceCardSelfAbility_"))
                    {
                        var text2 = text;
                        var length = "DiceCardSelfAbility_".Length;
                        text = text2.Substring(length, text2.Length - length);
                    }
                    AddUnique(CardScripts, text);
                }
                else if (name.Contains("DiceCardAbility"))
                {
                    if (text.StartsWith("DiceCardAbility_"))
                    {
                        var text2 = text;
                        var length = "DiceCardAbility_".Length;
                        text = text2.Substring(length, text2.Length - length);
                    }
                    AddUnique(DiceScripts, text);
                }
                else if (name.Contains("PassiveAbilityBase"))
                {
                    if (text.StartsWith("PassiveAbility_"))
                    {
                        var text2 = text;
                        var length = "PassiveAbility_".Length;
                        text = text2.Substring(length, text2.Length - length);
                    }
                    AddUnique(PassiveScripts, text);
                }
            }
        }
        catch
        {
        }
    }

    private static void AddUnique(List<string> list, string item)
    {
        if (!list.Contains(item))
        {
            list.Add(item);
        }
    }
}
