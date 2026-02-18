using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualBasic;
using Synthesis.Core.Log;

namespace Synthesis.Core.Tools;

public static class RichTextMenuBehavior
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached("IsEnabled",
        typeof(bool), typeof(RichTextMenuBehavior), new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(DependencyObject obj, bool value)
    {
        obj.SetValue(IsEnabledProperty, value);
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var textBox = d as TextBox;
        if (textBox != null && (bool)e.NewValue)
        {
            var textBox2 = textBox;
            if (textBox2.ContextMenu == null)
            {
                var contextMenu = textBox2.ContextMenu = new ContextMenu();
            }
            textBox.ContextMenuOpening += delegate { EnsureRichTextMenuItems(textBox); };
        }
    }

    private static void EnsureRichTextMenuItems(TextBox textBox)
    {
        var contextMenu = textBox.ContextMenu;
        if (contextMenu == null)
        {
            return;
        }
        foreach (var item in contextMenu.Items)
        {
            if (item is MenuItem { Tag: var tag } && tag?.ToString() == "RichTextRoot")
            {
                return;
            }
        }
        if (contextMenu.Items.Count > 0)
        {
            contextMenu.Items.Add(new Separator());
        }
        var menuItem2 = CreateMenuItem("\ud83d\udcdd 样式 (Style)", "RichTextRoot");
        menuItem2.Items.Add(CreateActionItem("\ud835\udc01  加粗 (Bold)", textBox, "b"));
        menuItem2.Items.Add(CreateActionItem("\ud835\udc3c  斜体 (Italic)", textBox, "i"));
        menuItem2.Items.Add(CreateActionItem("U\u0332  下划线 (Underline)", textBox, "u"));
        menuItem2.Items.Add(CreateActionItem("S\u0336  删除线 (Strike)", textBox, "s"));
        contextMenu.Items.Add(menuItem2);
        var menuItem3 = CreateMenuItem("\ud83c\udfa8 颜色 (Color)");
        menuItem3.Items.Add(CreateColorItem("\ud83d\udd34 红色 (Red)", textBox, "red"));
        menuItem3.Items.Add(CreateColorItem("\ud83d\udd35 蓝色 (Blue)", textBox, "blue"));
        menuItem3.Items.Add(CreateColorItem("\ud83d\udfe2 绿色 (Green)", textBox, "green"));
        menuItem3.Items.Add(CreateColorItem("\ud83d\udfe1 黄色 (Yellow)", textBox, "yellow"));
        menuItem3.Items.Add(CreateColorItem("\ud83d\udfe3 紫色 (Purple)", textBox, "purple"));
        menuItem3.Items.Add(CreateColorItem("⚪ 白色 (White)", textBox, "white"));
        menuItem3.Items.Add(CreateColorItem("⚫ 黑色 (Black)", textBox, "black"));
        var menuItem4 = new MenuItem
        {
            Header = "⚙\ufe0f 自定义 (Hex)..."
        };
        menuItem4.Click += delegate
        {
            var text = Interaction.InputBox("请输入颜色代码 (如 #FF0099):", "自定义颜色", "#");
            if (!string.IsNullOrWhiteSpace(text))
            {
                InsertTag(textBox, "color", text);
            }
        };
        menuItem3.Items.Add(menuItem4);
        contextMenu.Items.Add(menuItem3);
        var menuItem5 = CreateMenuItem("\ud83d\uddbc\ufe0f 图标 (Sprite)");
        menuItem5.Items.Add(CreateSpriteItem("⚔\ufe0f 斩击 (Slash)", textBox, "Slash"));
        menuItem5.Items.Add(CreateSpriteItem("\ud83d\udde1\ufe0f 突刺 (Penetrate)", textBox, "Penetrate"));
        menuItem5.Items.Add(CreateSpriteItem("\ud83d\udd28 打击 (Hit)", textBox, "Hit"));
        menuItem5.Items.Add(CreateSpriteItem("\ud83d\udee1\ufe0f 防御 (Guard)", textBox, "Guard"));
        menuItem5.Items.Add(CreateSpriteItem("\ud83d\udca8 闪避 (Evade)", textBox, "Evade"));
        menuItem5.Items.Add(new Separator());
        var menuItem6 = new MenuItem
        {
            Header = "⚙\ufe0f 自定义名称..."
        };
        menuItem6.Click += delegate
        {
            var text = Interaction.InputBox("请输入图片文件名 (不带.png):", "插入 Sprite");
            if (!string.IsNullOrWhiteSpace(text))
            {
                InsertTag(textBox, "sprite", null, "name=\"" + text + "\"");
            }
        };
        menuItem5.Items.Add(menuItem6);
        contextMenu.Items.Add(menuItem5);
        var menuItem7 = CreateMenuItem("\ud83d\udccf 大小 (Size)");
        menuItem7.Items.Add(CreateActionItem("Huge (60)", textBox, "size", "60"));
        menuItem7.Items.Add(CreateActionItem("Big (40)", textBox, "size", "40"));
        menuItem7.Items.Add(CreateActionItem("Normal (30)", textBox, "size", "30"));
        contextMenu.Items.Add(menuItem7);
    }

    private static MenuItem CreateMenuItem(string header, string tag = "") => new()
    {
        Header = header,
        Tag = tag
    };

    private static MenuItem CreateActionItem(string header, TextBox tb, string tag, string? param = null)
    {
        var menuItem = new MenuItem();
        menuItem.Header = header;
        menuItem.Click += delegate { InsertTag(tb, tag, param); };
        return menuItem;
    }

    private static MenuItem CreateSpriteItem(string header, TextBox tb, string spriteName)
    {
        var menuItem = new MenuItem();
        menuItem.Header = header;
        menuItem.Click += delegate { InsertTag(tb, "sprite", null, "name=\"" + spriteName + "\""); };
        return menuItem;
    }

    private static MenuItem CreateColorItem(string header, TextBox tb, string colorName)
    {
        var menuItem = new MenuItem
        {
            Header = header
        };
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(colorName);
            menuItem.Icon = new Rectangle
            {
                Width = 12.0,
                Height = 12.0,
                Fill = new SolidColorBrush(color),
                Stroke = Brushes.Gray,
                StrokeThickness = 1.0
            };
        }
        catch (Exception ex)
        {
            Logger.Warn(ex.Message);
        }
        menuItem.Click += delegate { InsertTag(tb, "color", colorName); };
        return menuItem;
    }

    private static void InsertTag(TextBox textBox, string tagName, string? param = null, string? customFullAttr = null)
    {
        var selectedText = textBox.SelectedText;
        var selectionStart = textBox.SelectionStart;
        var text = customFullAttr == null
            ? param == null ? "<" + tagName + ">" : $"<{tagName}={param}>"
            : $"<{tagName} {customFullAttr}>";
        var text2 = tagName == "sprite" ? "" : "</" + tagName + ">";
        textBox.SelectedText = text + selectedText + text2;
        if (string.IsNullOrEmpty(selectedText))
        {
            if (tagName == "sprite")
            {
                textBox.SelectionStart = selectionStart + text.Length;
            }
            else
            {
                textBox.SelectionStart = selectionStart + text.Length;
            }
            textBox.SelectionLength = 0;
        }
        else
        {
            textBox.SelectionStart = selectionStart;
            textBox.SelectionLength = text.Length + selectedText.Length + text2.Length;
        }
        textBox.Focus();
    }
}
