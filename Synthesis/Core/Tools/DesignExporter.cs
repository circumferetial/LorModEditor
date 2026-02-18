using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Synthesis.Feature.Enemy;

namespace Synthesis.Core.Tools;

public static class DesignExporter
{
    public static void ExportToMarkdown(ProjectManager manager, string outputPath)
    {
        var stringBuilder = new StringBuilder();
        var stringBuilder2 = stringBuilder;
        var stringBuilder3 = stringBuilder2;
        var handler = new StringBuilder.AppendInterpolatedStringHandler(11, 1, stringBuilder2);
        handler.AppendLiteral("# ");
        handler.AppendFormatted(manager.CurrentModId);
        handler.AppendLiteral(" - 角色设计文档");
        stringBuilder3.AppendLine(ref handler);
        stringBuilder2 = stringBuilder;
        var stringBuilder4 = stringBuilder2;
        handler = new StringBuilder.AppendInterpolatedStringHandler(8, 1, stringBuilder2);
        handler.AppendLiteral("> 导出时间: ");
        handler.AppendFormatted(DateTime.Now, "yyyy-MM-dd HH:mm");
        stringBuilder4.AppendLine(ref handler);
        stringBuilder.AppendLine("");
        stringBuilder.AppendLine("---");
        stringBuilder.AppendLine("");
        UnifiedEnemy[] array = (from x in manager.EnemyRepo.Items
            where !x.IsVanilla
            orderby x.Id
            select x).ToArray();
        if (array.Length == 0)
        {
            stringBuilder.AppendLine("*没有找到自定义敌人数据*");
        }
        var array2 = array;
        foreach (var enemy in array2)
        {
            stringBuilder2 = stringBuilder;
            var stringBuilder5 = stringBuilder2;
            handler = new StringBuilder.AppendInterpolatedStringHandler(12, 2, stringBuilder2);
            handler.AppendLiteral("# \ud83c\udfad 角色: [");
            handler.AppendFormatted(enemy.Id);
            handler.AppendLiteral("] ");
            handler.AppendFormatted(EscapeMarkdown(enemy.Name));
            stringBuilder5.AppendLine(ref handler);
            var unifiedBook = manager.BookRepo.Items.FirstOrDefault(b => b.Id == enemy.Id);
            if (unifiedBook != null)
            {
                stringBuilder2 = stringBuilder;
                var stringBuilder6 = stringBuilder2;
                handler = new StringBuilder.AppendInterpolatedStringHandler(15, 2, stringBuilder2);
                handler.AppendLiteral("## \ud83d\udcd6 核心书页: [");
                handler.AppendFormatted(unifiedBook.Id);
                handler.AppendLiteral("] ");
                handler.AppendFormatted(EscapeMarkdown(unifiedBook.Name));
                stringBuilder6.AppendLine(ref handler);
                stringBuilder2 = stringBuilder;
                var stringBuilder7 = stringBuilder2;
                handler = new StringBuilder.AppendInterpolatedStringHandler(26, 4, stringBuilder2);
                handler.AppendLiteral("- **数值**: HP ");
                handler.AppendFormatted(unifiedBook.HP);
                handler.AppendLiteral(" | 混乱 ");
                handler.AppendFormatted(unifiedBook.Break);
                handler.AppendLiteral(" | 速度 ");
                handler.AppendFormatted(unifiedBook.SpeedMin);
                handler.AppendLiteral("-");
                handler.AppendFormatted(unifiedBook.Speed);
                stringBuilder7.AppendLine(ref handler);
                stringBuilder2 = stringBuilder;
                var stringBuilder8 = stringBuilder2;
                handler = new StringBuilder.AppendInterpolatedStringHandler(24, 3, stringBuilder2);
                handler.AppendLiteral("- **抗性 (物理)**: 斩");
                handler.AppendFormatted(unifiedBook.SResist);
                handler.AppendLiteral(" / 穿");
                handler.AppendFormatted(unifiedBook.PResist);
                handler.AppendLiteral(" / 打");
                handler.AppendFormatted(unifiedBook.HResist);
                stringBuilder8.AppendLine(ref handler);
                stringBuilder2 = stringBuilder;
                var stringBuilder9 = stringBuilder2;
                handler = new StringBuilder.AppendInterpolatedStringHandler(24, 3, stringBuilder2);
                handler.AppendLiteral("- **抗性 (混乱)**: 斩");
                handler.AppendFormatted(unifiedBook.SBResist);
                handler.AppendLiteral(" / 穿");
                handler.AppendFormatted(unifiedBook.PBResist);
                handler.AppendLiteral(" / 打");
                handler.AppendFormatted(unifiedBook.HBResist);
                stringBuilder9.AppendLine(ref handler);
                stringBuilder.AppendLine("");
                stringBuilder.AppendLine("### ⚡ 核心被动");
                if (unifiedBook.Passives.Count == 0)
                {
                    stringBuilder.AppendLine("> *无*");
                }
                foreach (var pid in unifiedBook.Passives)
                {
                    var unifiedPassive = manager.PassiveRepo.Items.FirstOrDefault(p => p.GlobalId == pid);
                    if (unifiedPassive != null)
                    {
                        stringBuilder2 = stringBuilder;
                        var stringBuilder10 = stringBuilder2;
                        handler = new StringBuilder.AppendInterpolatedStringHandler(15, 2, stringBuilder2);
                        handler.AppendLiteral("**[");
                        handler.AppendFormatted(EscapeMarkdown(unifiedPassive.Name));
                        handler.AppendLiteral("]** (Cost: ");
                        handler.AppendFormatted(unifiedPassive.Cost);
                        handler.AppendLiteral(")");
                        stringBuilder10.AppendLine(ref handler);
                        if (!string.IsNullOrWhiteSpace(unifiedPassive.Desc))
                        {
                            var value = SearchLineBreaks().Replace(EscapeMarkdown(unifiedPassive.Desc), "  \n> ");
                            stringBuilder2 = stringBuilder;
                            var stringBuilder11 = stringBuilder2;
                            handler = new StringBuilder.AppendInterpolatedStringHandler(2, 1, stringBuilder2);
                            handler.AppendLiteral("> ");
                            handler.AppendFormatted(value);
                            stringBuilder11.AppendLine(ref handler);
                        }
                    }
                    else
                    {
                        stringBuilder2 = stringBuilder;
                        var stringBuilder12 = stringBuilder2;
                        handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
                        handler.AppendLiteral("**[");
                        handler.AppendFormatted(pid);
                        handler.AppendLiteral("]** (未知/原版)");
                        stringBuilder12.AppendLine(ref handler);
                    }
                    stringBuilder.AppendLine("");
                }
            }
            else
            {
                stringBuilder.AppendLine("> *未绑定核心书页或书页 ID 无效*");
            }
            stringBuilder.AppendLine("");
            stringBuilder.AppendLine("---");
            stringBuilder.AppendLine("");
            stringBuilder2 = stringBuilder;
            var stringBuilder13 = stringBuilder2;
            handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
            handler.AppendLiteral("## \ud83c\udccf 战斗卡组 (");
            handler.AppendFormatted(enemy.DeckCardIds.Count);
            handler.AppendLiteral(" 张)");
            stringBuilder13.AppendLine(ref handler);
            stringBuilder.AppendLine("");
            foreach (var group in from id in enemy.DeckCardIds
                     group id by id
                     into g
                     select new
                     {
                         Id = g.Key,
                         Count = g.Count()
                     }
                     into x
                     orderby x.Id.ItemId
                     select x)
            {
                var unifiedCard = manager.CardRepo.Items.FirstOrDefault(c => c.GlobalId == group.Id && !c.IsVanilla) ??
                                  manager.CardRepo.Items.FirstOrDefault(c => c.GlobalId == group.Id);
                if (unifiedCard != null)
                {
                    stringBuilder2 = stringBuilder;
                    var stringBuilder14 = stringBuilder2;
                    handler = new StringBuilder.AppendInterpolatedStringHandler(16, 3, stringBuilder2);
                    handler.AppendLiteral("### [");
                    handler.AppendFormatted(unifiedCard.Cost);
                    handler.AppendLiteral("费] **");
                    handler.AppendFormatted(EscapeMarkdown(unifiedCard.Name));
                    handler.AppendLiteral("** (x");
                    handler.AppendFormatted(group.Count);
                    handler.AppendLiteral(")");
                    stringBuilder14.AppendLine(ref handler);
                    if (!string.IsNullOrEmpty(unifiedCard.Script))
                    {
                        var text = FindAbilityDesc(manager, unifiedCard.Script);
                        if (!string.IsNullOrEmpty(text))
                        {
                            var value2 = SearchLineBreaks().Replace(EscapeMarkdown(text), "  \n> ");
                            stringBuilder2 = stringBuilder;
                            var stringBuilder15 = stringBuilder2;
                            handler = new StringBuilder.AppendInterpolatedStringHandler(4, 1, stringBuilder2);
                            handler.AppendLiteral("> *");
                            handler.AppendFormatted(value2);
                            handler.AppendLiteral("*");
                            stringBuilder15.AppendLine(ref handler);
                        }
                    }
                    stringBuilder.AppendLine("");
                    if (unifiedCard.Behaviours.Count > 0)
                    {
                        stringBuilder.AppendLine("| 骰子 | 细节 | 类型 | 效果 |");
                        stringBuilder.AppendLine("| :--- | :--- | :--- | :--- |");
                        foreach (var behaviour in unifiedCard.Behaviours)
                        {
                            var value3 = "-";
                            if (!string.IsNullOrEmpty(behaviour.Script))
                            {
                                var text2 = FindAbilityDesc(manager, behaviour.Script);
                                value3 = !string.IsNullOrEmpty(text2)
                                    ? SearchLineBreaks().Replace(EscapeMarkdown(text2), "<br/>")
                                    : "`" + behaviour.Script + "`";
                            }
                            stringBuilder2 = stringBuilder;
                            var stringBuilder16 = stringBuilder2;
                            handler = new StringBuilder.AppendInterpolatedStringHandler(14, 5, stringBuilder2);
                            handler.AppendLiteral("| ");
                            handler.AppendFormatted(behaviour.Min);
                            handler.AppendLiteral("-");
                            handler.AppendFormatted(behaviour.Dice);
                            handler.AppendLiteral(" | ");
                            handler.AppendFormatted(behaviour.Detail);
                            handler.AppendLiteral(" | ");
                            handler.AppendFormatted(behaviour.Type);
                            handler.AppendLiteral(" | ");
                            handler.AppendFormatted(value3);
                            handler.AppendLiteral(" |");
                            stringBuilder16.AppendLine(ref handler);
                        }
                    }
                    stringBuilder.AppendLine("");
                    stringBuilder.AppendLine("***");
                }
                else
                {
                    stringBuilder2 = stringBuilder;
                    var stringBuilder17 = stringBuilder2;
                    handler = new StringBuilder.AppendInterpolatedStringHandler(19, 2, stringBuilder2);
                    handler.AppendLiteral("### [未知卡牌] ID: ");
                    handler.AppendFormatted(group.Id);
                    handler.AppendLiteral(" (x");
                    handler.AppendFormatted(group.Count);
                    handler.AppendLiteral(")");
                    stringBuilder17.AppendLine(ref handler);
                }
                stringBuilder.AppendLine("");
            }
            stringBuilder.AppendLine("");
        }
        File.WriteAllText(outputPath, stringBuilder.ToString());
    }

    private static string EscapeMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "";
        }
        return text.Replace("[", "\\[").Replace("]", "\\]").Replace("|", "\\|");
    }

    private static string FindAbilityDesc(ProjectManager manager, string scriptId)
    {
        if (string.IsNullOrEmpty(scriptId))
        {
            return "";
        }
        return (from a in manager.AbilityRepo.Items
            where a.Id == scriptId
            orderby a.IsVanilla
            select a).FirstOrDefault()?.Desc ?? "";
    }

    private static Regex SearchLineBreaks() => new("\\r\\n?|\\n", RegexOptions.Compiled);
}
