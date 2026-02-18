using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Synthesis.Core.Tools;

public static class RichTextMenuBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled",
        typeof(bool),
        typeof(RichTextMenuBehavior),
        new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox || !(bool)e.NewValue) return;
        textBox.ContextMenu ??= new ContextMenu();
        // 每次打开菜单时检查是否需要添加选项
        textBox.ContextMenuOpening += (_, _) => EnsureRichTextMenuItems(textBox);
    }

    private static void EnsureRichTextMenuItems(TextBox textBox)
    {
        var menu = textBox.ContextMenu;
        // 防止重复添加
        if (menu == null) return;
        foreach (var item in menu.Items)
        {
            if (item is MenuItem mi && mi.Tag?.ToString() == "RichTextRoot") return;
        }

        if (menu.Items.Count > 0) menu.Items.Add(new Separator());

        // 样式
        var styleItem = CreateMenuItem("📝 样式 (Style)", "RichTextRoot");
        styleItem.Items.Add(CreateActionItem("𝐁  加粗 (Bold)", textBox, "b"));
        styleItem.Items.Add(CreateActionItem("𝐼  斜体 (Italic)", textBox, "i"));
        menu.Items.Add(styleItem);

        // 颜色
        var colorItem = CreateMenuItem("🎨 颜色 (Color)");
        colorItem.Items.Add(CreateColorItem("🔴 红色 (Red)", textBox, "red"));
        colorItem.Items.Add(CreateColorItem("🔵 蓝色 (Blue)", textBox, "blue"));
        colorItem.Items.Add(CreateColorItem("🟢 绿色 (Green)", textBox, "green"));
        colorItem.Items.Add(CreateColorItem("🟡 黄色 (Yellow)", textBox, "yellow"));
        colorItem.Items.Add(CreateColorItem("🟣 紫色 (Purple)", textBox, "purple"));
        colorItem.Items.Add(CreateColorItem("⚪ 白色 (White)", textBox, "white"));
        colorItem.Items.Add(CreateColorItem("⚫ 黑色 (Black)", textBox, "black"));
        menu.Items.Add(colorItem);

        // 大小
        var sizeItem = CreateMenuItem("📏 大小 (Size)");
        sizeItem.Items.Add(CreateActionItem("Huge (60)", textBox, "size", "60"));
        sizeItem.Items.Add(CreateActionItem("Big (40)", textBox, "size", "40"));
        sizeItem.Items.Add(CreateActionItem("Normal (30)", textBox, "size", "30"));
        sizeItem.Items.Add(CreateActionItem("Small (20)", textBox, "size", "20"));
        menu.Items.Add(sizeItem);
    }

    private static MenuItem CreateMenuItem(string header, string tag = "") => new() { Header = header, Tag = tag };

    private static MenuItem CreateActionItem(string header, TextBox tb, string tag, string? param = null)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => InsertTag(tb, tag, param);
        return item;
    }
    
    private static MenuItem CreateColorItem(string header, TextBox tb, string? colorName)
    {
        var item = new MenuItem { Header = header };
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(colorName);
            item.Icon = new System.Windows.Shapes.Rectangle
            {
                Width = 12, Height = 12, Fill = new SolidColorBrush(color), Stroke = Brushes.Gray, StrokeThickness = 1
            };
        }
        catch (Exception ex)
        {
            Log.Logger.Warn(ex.Message);
        }
        item.Click += (_, _) => InsertTag(tb, "color", colorName);
        return item;
    }

    private static void InsertTag(TextBox textBox, string tagName, string? param = null)
    {
        string selectedText = textBox.SelectedText;
        int selectionStart = textBox.SelectionStart;
        string openTag = param == null ? $"<{tagName}>" : $"<{tagName}={param}>";
        string closeTag = $"</{tagName}>";

        textBox.SelectedText = $"{openTag}{selectedText}{closeTag}";

        if (string.IsNullOrEmpty(selectedText))
        {
            textBox.SelectionStart = selectionStart + openTag.Length;
            textBox.SelectionLength = 0;
        }
        else
        {
            textBox.SelectionStart = selectionStart;
            textBox.SelectionLength = openTag.Length + selectedText.Length + closeTag.Length;
        }
        textBox.Focus();
    }
}

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