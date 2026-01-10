using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace LorModEditor.Core.Services;

public static partial class DesignExporter
{
    public static void ExportToMarkdown(ProjectManager manager, string outputPath)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# {manager.CurrentModId} - 角色设计文档");
        sb.AppendLine($"> 导出时间: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine("");// 空行
        sb.AppendLine("---");
        sb.AppendLine("");// 空行

        var modEnemies = manager.EnemyRepo.Items.Where(x => !x.IsVanilla).OrderBy(x => x.Id).ToArray();

        if (modEnemies.Length == 0)
        {
            sb.AppendLine("*没有找到自定义敌人数据*");
        }

        foreach (var enemy in modEnemies)
        {
            // === 1. 角色头衔 ===
            sb.AppendLine($"# 🎭 角色: [{enemy.Id}] {EscapeMarkdown(enemy.Name)}");

            // === 2. 核心书页 ===
            var book = manager.BookRepo.Items.FirstOrDefault(b => b.Id == enemy.Id);
            if (book != null)
            {
                sb.AppendLine($"## 📖 核心书页: [{book.Id}] {EscapeMarkdown(book.Name)}");
                sb.AppendLine($"- **数值**: HP {book.HP} | 混乱 {book.Break} | 速度 {book.SpeedMin}-{book.Speed}");
                sb.AppendLine($"- **抗性 (物理)**: 斩{book.SResist} / 穿{book.PResist} / 打{book.HResist}");
                sb.AppendLine($"- **抗性 (混乱)**: 斩{book.SBResist} / 穿{book.PBResist} / 打{book.HBResist}");
                sb.AppendLine("");// 空行

                sb.AppendLine("### ⚡ 核心被动");
                if (book.Passives.Count == 0) sb.AppendLine("> *无*");

                foreach (var pid in book.Passives)
                {
                    var passive = manager.PassiveRepo.Items.FirstOrDefault(p => p.GlobalId == pid);
                    if (passive != null)
                    {
                        sb.AppendLine($"**[{EscapeMarkdown(passive.Name)}]** (Cost: {passive.Cost})");
                        if (!string.IsNullOrWhiteSpace(passive.Desc))
                        {
                            // 【修复】保留被动描述的换行
                            // Markdown 引用换行需要: "  \n> " (两个空格+换行+大于号)
                            var fmtDesc = SearchLineBreaks().Replace(EscapeMarkdown(passive.Desc), "  \n> ");
                            sb.AppendLine($"> {fmtDesc}");
                        }
                    }
                    else
                    {
                        sb.AppendLine($"**[{pid}]** (未知/原版)");
                    }
                    sb.AppendLine("");// 每个被动之间空一行，更清晰
                }
            }
            else
            {
                sb.AppendLine("> *未绑定核心书页或书页 ID 无效*");
            }
            sb.AppendLine("");
            sb.AppendLine("---");
            sb.AppendLine("");

            // === 3. 卡组信息 ===
            sb.AppendLine($"## 🃏 战斗卡组 ({enemy.DeckCardIds.Count} 张)");
            sb.AppendLine("");// 空行

            var deckGroups = enemy.DeckCardIds
                .GroupBy(id => id)
                .Select(g => new { Id = g.Key, Count = g.Count() })
                .OrderBy(x => x.Id.ItemId);

            foreach (var group in deckGroups)
            {
                var card = manager.CardRepo.Items.FirstOrDefault(c => c.GlobalId == group.Id && !c.IsVanilla)
                           ?? manager.CardRepo.Items.FirstOrDefault(c => c.GlobalId == group.Id);

                if (card != null)
                {
                    // 标题
                    sb.AppendLine($"### [{card.Cost}费] **{EscapeMarkdown(card.Name)}** (x{group.Count})");

                    // 卡牌主脚本描述
                    if (!string.IsNullOrEmpty(card.Script))
                    {
                        var scriptDesc = FindAbilityDesc(manager, card.Script);
                        if (!string.IsNullOrEmpty(scriptDesc))
                        {
                            // 【修复】卡牌描述换行
                            var fmtDesc = SearchLineBreaks().Replace(EscapeMarkdown(scriptDesc), "  \n> ");
                            sb.AppendLine($"> *{fmtDesc}*");
                        }
                    }

                    // 必须加空行，否则 Markdown 可能不渲染下面的表格
                    sb.AppendLine("");

                    // 骰子列表
                    if (card.Behaviours.Count > 0)
                    {
                        sb.AppendLine("| 骰子 | 细节 | 类型 | 效果 |");
                        sb.AppendLine("| :--- | :--- | :--- | :--- |");

                        foreach (var d in card.Behaviours)
                        {
                            var effectText = "-";
                            if (!string.IsNullOrEmpty(d.Script))
                            {
                                var foundDesc = FindAbilityDesc(manager, d.Script);
                                // 【修复】表格内换行必须用 <br/>
                                effectText = !string.IsNullOrEmpty(foundDesc)
                                    ? SearchLineBreaks().Replace(EscapeMarkdown(foundDesc), "<br/>")
                                    : $"`{d.Script}`";
                            }

                            sb.AppendLine($"| {d.Min}-{d.Dice} | {d.Detail} | {d.Type} | {effectText} |");
                        }
                    }
                    sb.AppendLine("");// 卡牌结束后空行
                    sb.AppendLine("***");// 分隔线
                    // 空行
                }
                else
                {
                    sb.AppendLine($"### [未知卡牌] ID: {group.Id} (x{group.Count})");
                }
                sb.AppendLine("");// 空行
            }

            sb.AppendLine("");
        }

        File.WriteAllText(outputPath, sb.ToString());
    }

    private static string EscapeMarkdown(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text
            .Replace("[", "\\[")
            .Replace("]", "\\]")
            .Replace("|", "\\|");
    }

    private static string FindAbilityDesc(ProjectManager manager, string scriptId)
    {
        if (string.IsNullOrEmpty(scriptId)) return "";

        var ability = manager.AbilityRepo.Items
            .Where(a => a.Id == scriptId)
            .OrderBy(a => a.IsVanilla)
            .FirstOrDefault();

        return ability?.Desc ?? "";
    }

    [GeneratedRegex(@"\r\n?|\n")]
    private static partial Regex SearchLineBreaks();
}