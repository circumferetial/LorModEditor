namespace Synthesis.Core.Tools;

public static class SafeCast
{
    public static int ToInt(this string? value, int defaultValue = 0)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }
        if (!int.TryParse(value, out var result))
        {
            return defaultValue;
        }
        return result;
    }

    public static bool ToBool(this string? value, bool defaultValue = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }
        if (!bool.TryParse(value, out var result))
        {
            return defaultValue;
        }
        return result;
    }

    public static T ToEnum<T>(this string? value, T defaultValue) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }
        if (!Enum.TryParse<T>(value, true, out var result))
        {
            return defaultValue;
        }
        return result;
    }

    public static double ToDouble(this string? value, double defaultValue)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }
        if (!double.TryParse(value, out var result))
        {
            return defaultValue;
        }
        return result;
    }

    public static string Format(this bool value) => value.ToString().ToLower();

    public static string Format(this object? value) => value?.ToString() ?? "";

    public static string Format(this int value) => value.ToString();
}
