using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Synthesis.Core.Tools;

public static partial class UnityRichTextHelper
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached("Text", typeof(string),
        typeof(UnityRichTextHelper), new PropertyMetadata(string.Empty, OnTextChanged));

    private static readonly Regex TagRegex = MyRegex();

    private static readonly Regex AttributeRegex = MyRegex1();

    public static Func<string, ImageSource?>? SpriteResolver { get; set; }

    public static string GetText(DependencyObject obj) => (string)obj.GetValue(TextProperty);

    public static void SetText(DependencyObject obj, string value)
    {
        obj.SetValue(TextProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock) return;
        textBlock.Inlines.Clear();
        var text = e.NewValue as string;
        if (!string.IsNullOrEmpty(text))
        {
            ParseUnityRichText(text, textBlock.Inlines, textBlock.FontSize, textBlock.Foreground);
        }
    }

    private static void ParseUnityRichText(string input, InlineCollection inlines, double baseFontSize,
        Brush baseForeground)
    {
        if (input.IndexOf('<') < 0)
        {
            AddRun(inlines, input, default, baseFontSize, baseForeground);
            return;
        }

        var matchCollection = TagRegex.Matches(input);
        var num = 0;
        var stack = new Stack<StyleState>();
        stack.Push(default);
        foreach (Match item2 in matchCollection)
        {
            if (item2.Index > num)
            {
                var text = input.Substring(num, item2.Index - num);
                AddRun(inlines, text, stack.Peek(), baseFontSize, baseForeground);
            }
            var value = item2.Value;
            var text2 = item2.Groups["tag"].Value.ToLower();
            if (text2 == "sprite")
            {
                var match2 = AttributeRegex.Match(value);
                if (match2.Success && match2.Groups["name"].Value == "name")
                {
                    var value2 = match2.Groups["val"].Value;
                    AddSprite(inlines, value2, stack.Peek(), baseFontSize);
                }
                num = item2.Index + item2.Length;
                continue;
            }
            if (text2.StartsWith('/'))
            {
                if (stack.Count > 1)
                {
                    stack.Pop();
                }
            }
            else
            {
                var item = stack.Peek().Clone();
                var flag = true;
                var text3 = "";
                if (value.Contains('='))
                {
                    var num2 = value.IndexOf('=');
                    var num3 = value.IndexOf('>');
                    if (num3 > num2)
                    {
                        text3 = value.Substring(num2 + 1, num3 - num2 - 1);
                    }
                }
                switch (text2)
                {
                    case "b":
                        item.Bold = true;
                        break;
                    case "i":
                        item.Italic = true;
                        break;
                    case "u":
                        item.Underline = true;
                        break;
                    case "s":
                        item.Strikethrough = true;
                        break;
                    case "size":
                    {
                        if (double.TryParse(text3, out var result))
                        {
                            item.Size = result;
                        }
                        break;
                    }
                    case "color":
                        item.Foreground = ParseColor(text3);
                        break;
                    case "alpha":
                        item.Alpha = text3;
                        break;
                    default:
                        flag = false;
                        break;
                }
                if (flag)
                {
                    stack.Push(item);
                }
            }
            num = item2.Index + item2.Length;
        }
        if (num < input.Length)
        {
            AddRun(inlines, input[num..], stack.Peek(), baseFontSize, baseForeground);
        }
    }

    private static void AddRun(InlineCollection inlines, string text, StyleState state, double baseFontSize,
        Brush baseForeground)
    {
        var run = new Run(text);
        if (state.Bold)
        {
            run.FontWeight = FontWeights.Bold;
        }
        if (state.Italic)
        {
            run.FontStyle = FontStyles.Italic;
        }
        var textDecorationCollection = new TextDecorationCollection();
        if (state.Underline)
        {
            textDecorationCollection.Add(TextDecorations.Underline);
        }
        if (state.Strikethrough)
        {
            textDecorationCollection.Add(TextDecorations.Strikethrough);
        }
        if (textDecorationCollection.Count > 0)
        {
            run.TextDecorations = textDecorationCollection;
        }
        var brush = state.Foreground ?? baseForeground;
        if (!string.IsNullOrEmpty(state.Alpha) && brush is SolidColorBrush { Color: var color })
        {
            color.A = ParseAlpha(state.Alpha);
            run.Foreground = new SolidColorBrush(color);
        }
        else
        {
            run.Foreground = brush;
        }
        run.FontSize = state.Size ?? baseFontSize;
        inlines.Add(run);
    }

    private static void AddSprite(InlineCollection inlines, string spriteName, StyleState state, double baseFontSize)
    {
        var imageSource = SpriteResolver?.Invoke(spriteName);
        if (imageSource != null)
        {
            var item = new InlineUIContainer(new Image
            {
                Source = imageSource,
                Width = state.Size ?? baseFontSize,
                Height = state.Size ?? baseFontSize,
                Stretch = Stretch.Uniform
            })
            {
                BaselineAlignment = BaselineAlignment.Center
            };
            inlines.Add(item);
        }
    }

    private static SolidColorBrush? ParseColor(string colorStr)
    {
        try
        {
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorStr));
        }
        catch
        {
            return null;
        }
    }

    private static byte ParseAlpha(string alphaHex)
    {
        try
        {
            return Convert.ToByte(alphaHex.Replace("#", ""), 16);
        }
        catch
        {
            return byte.MaxValue;
        }
    }


    [GeneratedRegex("(?<name>[a-zA-Z0-9]+)=\"(?<val>[^\"]*)\"", RegexOptions.Compiled)]
    private static partial Regex MyRegex1();

    [GeneratedRegex("<(?<tag>/?[a-zA-Z0-9]+)(?:(?:\\s+[a-zA-Z0-9]+=\"[^\"]*\")*|(?:\\=[^>]+))?\\s*/?>",
        RegexOptions.Compiled)]
    private static partial Regex MyRegex();

    private struct StyleState
    {
        public bool Bold;

        public bool Italic;

        public bool Underline;

        public bool Strikethrough;

        public Brush? Foreground;

        public double? Size;

        public string? Alpha;

        public StyleState Clone() => new()
        {
            Bold = Bold,
            Italic = Italic,
            Underline = Underline,
            Strikethrough = Strikethrough,
            Foreground = Foreground,
            Size = Size,
            Alpha = Alpha
        };
    }
}
