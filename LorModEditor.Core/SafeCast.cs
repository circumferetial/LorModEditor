namespace LorModEditor.Core;

/// <summary>
///     提供安全的类型转换方法，失败时返回默认值，绝不抛出异常。
/// </summary>
public static class SafeCast
{
    // --- 1. 转整数 (Int) ---
    public static int ToInt(this string? value, int defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        // 能够处理 "123" 也能抗住 "abc"
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    // --- 2. 转布尔 (Bool) ---
    public static bool ToBool(this string? value, bool defaultValue = false)
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        // 能够处理 "true", "True", "TRUE"
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    // --- 3. 转枚举 (Enum) ---
    public static T ToEnum<T>(this string? value, T defaultValue) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return defaultValue;
        // ignoreCase: true 表示忽略大小写，"weak" 和 "Weak" 都能识别
        return Enum.TryParse(value, true, out T result) ? result : defaultValue;
    }

    // --- 4. 格式化输出 (Format) ---
    // 专门处理写回 XML 时的格式问题

    // 游戏 XML 习惯用小写 "true"/"false"
    public static string Format(this bool value) => value.ToString().ToLower();

    // 其他类型直接 ToString 即可
    public static string Format(this object? value) => value?.ToString() ?? "";
    public static string Format(int value) => value.ToString();
}