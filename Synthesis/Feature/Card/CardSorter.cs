using System.Collections;

namespace Synthesis.Feature.Card;

internal class CardSorter : IComparer
{
    public int Compare(object? x, object? y)
    {
        if (x is not UnifiedCard cardX || y is not UnifiedCard cardY) return 0;

        // 1. Mod 内容置顶 (IsVanilla: False < True)
        if (cardX.IsVanilla != cardY.IsVanilla)
        {
            return cardX.IsVanilla.CompareTo(cardY.IsVanilla);
        }

        // 2. ID 数值排序
        var xIsInt = int.TryParse(cardX.Id, out var idX);
        var yIsInt = int.TryParse(cardY.Id, out var idY);

        if (xIsInt && yIsInt)
        {
            return idX.CompareTo(idY);
        }

        // 3. 兜底字符串排序
        return string.Compare(cardX.Id, cardY.Id, StringComparison.OrdinalIgnoreCase);
    }
}