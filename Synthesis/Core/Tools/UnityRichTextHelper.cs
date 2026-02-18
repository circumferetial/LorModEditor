using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Synthesis.Core.Tools;

public static class UnityRichTextHelper
{
    // 定义附加属性 Text，让 TextBlock 可以直接绑定
    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text",
        typeof(string),
        typeof(UnityRichTextHelper),
        new PropertyMetadata(string.Empty, OnTextChanged));

    public static string GetText(DependencyObject obj) => (string)obj.GetValue(TextProperty);
    public static void SetText(DependencyObject obj, string value) => obj.SetValue(TextProperty, value);

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock textBlock)
        {
            textBlock.Inlines.Clear();
            var text = e.NewValue as string;
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                ParseUnityRichText(text, textBlock.Inlines, textBlock.FontSize);
            }
            catch
            {
                // 如果解析崩了，至少显示纯文本
                textBlock.Text = text;
            }
        }
    }

    // 正则：匹配 <tag=val> 或 <tag> 或 </tag>
    private static readonly Regex TagRegex = new(@"<(?<tag>/?[a-zA-Z0-9]+)(?:=(?<val>[^>]+))?>", RegexOptions.Compiled);

    private struct StyleState
    {
        public bool Bold;
        public bool Italic;
        public Brush? Foreground;
        public double? Size;

        public StyleState Clone() => new()
        {
            Bold = this.Bold,
            Italic = this.Italic,
            Foreground = this.Foreground,
            Size = this.Size
        };
    }

    private static void ParseUnityRichText(string input, InlineCollection inlines, double baseFontSize)
    {
        var matches = TagRegex.Matches(input);
        int lastIndex = 0;
        var styleStack = new Stack<StyleState>();
        styleStack.Push(new StyleState()); // 默认状态

        foreach (Match match in matches)
        {
            // 1. 添加标签前的文本
            if (match.Index > lastIndex)
            {
                string plainText = input.Substring(lastIndex, match.Index - lastIndex);
                AddRun(inlines, plainText, styleStack.Peek(), baseFontSize);
            }

            // 2. 处理标签
            string tagName = match.Groups["tag"].Value.ToLower();
            string val = match.Groups["val"].Value;

            if (tagName.StartsWith("/"))
            {
                // 关闭标签：弹栈
                if (styleStack.Count > 1) styleStack.Pop();
            }
            else
            {
                // 开启标签：压栈
                var currentState = styleStack.Peek().Clone();
                bool isNewState = true;

                switch (tagName)
                {
                    case "b": currentState.Bold = true; break;
                    case "i": currentState.Italic = true; break;
                    case "size": 
                        if (double.TryParse(val, out var s)) currentState.Size = s; 
                        break;
                    case "color": 
                        currentState.Foreground = ParseColor(val); 
                        break;
                    default: isNewState = false; break; // 忽略未知标签
                }

                if (isNewState) styleStack.Push(currentState);
            }

            lastIndex = match.Index + match.Length;
        }

        // 3. 添加剩余文本
        if (lastIndex < input.Length)
        {
            AddRun(inlines, input.Substring(lastIndex), styleStack.Peek(), baseFontSize);
        }
    }

    private static void AddRun(InlineCollection inlines, string text, StyleState state, double baseFontSize)
    {
        var run = new Run(text);
        if (state.Bold) run.FontWeight = FontWeights.Bold;
        if (state.Italic) run.FontStyle = FontStyles.Italic;
        if (state.Foreground != null) run.Foreground = state.Foreground;
        // 如果没有指定size，就跟随TextBlock的默认大小，否则使用指定大小
        run.FontSize = state.Size ?? baseFontSize;
        
        inlines.Add(run);
    }

    private static Brush? ParseColor(string colorStr)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(colorStr);
            return new SolidColorBrush(color);
        }
        catch { return null; }
    }
}