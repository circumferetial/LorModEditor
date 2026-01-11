using System.Collections;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using LorModEditor.Core;

namespace LorModEditor.Views.Components;

public partial class ImageSelector
{
    // ==========================================
    // 依赖属性定义 (API)
    // ==========================================

    // 1. ProjectManager (必填，用于加载图片)
    public static readonly DependencyProperty ManagerProperty =
        DependencyProperty.Register(nameof(Manager), typeof(ProjectManager), typeof(ImageSelector),
            new PropertyMetadata(null, OnManagerChanged));

    // 2. 选中的图片名 (双向绑定)
    public static readonly DependencyProperty SelectedImageNameProperty =
        DependencyProperty.Register(nameof(SelectedImageName), typeof(string), typeof(ImageSelector),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnImageNameChanged));

    // 3. 图片列表源 (Items)
    public static readonly DependencyProperty ImageListProperty =
        DependencyProperty.Register(nameof(ImageList), typeof(IEnumerable), typeof(ImageSelector));

    // 4. 标题文本
    public static readonly DependencyProperty LabelTextProperty =
        DependencyProperty.Register(nameof(LabelText), typeof(string), typeof(ImageSelector),
            new PropertyMetadata("Image:"));

    public ImageSelector()
    {
        InitializeComponent();
    }

    public ProjectManager Manager
    {
        get => (ProjectManager)GetValue(ManagerProperty);
        set => SetValue(ManagerProperty, value);
    }

    public string SelectedImageName
    {
        get => (string)GetValue(SelectedImageNameProperty);
        set => SetValue(SelectedImageNameProperty, value);
    }

    public IEnumerable ImageList
    {
        get => (IEnumerable)GetValue(ImageListProperty);
        set => SetValue(ImageListProperty, value);
    }

    public string LabelText
    {
        get => (string)GetValue(LabelTextProperty);
        set => SetValue(LabelTextProperty, value);
    }

    // 5. 预览图尺寸 (普通属性即可，不用DP，除非你想绑定)
    public double PreviewWidth { get; set; } = 200;
    public double PreviewHeight { get; set; } = 150;

    // ==========================================
    // 逻辑实现
    // ==========================================

    // 当外部 Manager 改变时 (比如切换 Tab)，尝试刷新图片
    private static void OnManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ImageSelector)d;
        control.LoadImage(control.SelectedImageName);
    }

    // 当外部 SelectedImageName 改变时 (比如切换选中卡牌)，刷新图片
    private static void OnImageNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (ImageSelector)d;
        control.LoadImage(e.NewValue as string);
    }

    private void LoadImage(string? name)
    {
        // 如果 Manager 还没注入，或者名字为空，清空图片
        if (string.IsNullOrEmpty(name))
        {
            PreviewImage?.Source = null;
            return;
        }

        try
        {
            var path = Manager.GetArtworkPath(name);
            if (path != null && File.Exists(path))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;// 解除文件占用
                bitmap.EndInit();
                PreviewImage.Source = bitmap;
            }
            else
            {
                PreviewImage.Source = null;
            }
        }
        catch
        {
            PreviewImage?.Source = null;
        }
    }

    // --- 交互逻辑 ---

    private void ImageCombo_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (e.OriginalSource is not TextBox tb) return;

        // 1. 过滤下拉框
        if (ImageCombo.ItemsSource != null)
        {
            var view = CollectionViewSource.GetDefaultView(ImageCombo.ItemsSource);
            view?.Filter = o =>
            {
                if (string.IsNullOrEmpty(tb.Text)) return true;
                if (o is string s) return s.Contains(tb.Text, StringComparison.OrdinalIgnoreCase);
                return false;
            };
        }

        // 2. 自动展开
        if (ImageCombo.IsKeyboardFocusWithin && !ImageCombo.IsDropDownOpen)
            ImageCombo.IsDropDownOpen = true;

        // 3. 实时刷新图片
        LoadImage(tb.Text);
    }

    private void ImageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ImageCombo.SelectedItem is string name)
        {
            LoadImage(name);

            // 取消全选体验优化
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (ImageCombo.Template.FindName("PART_EditableTextBox", ImageCombo) is TextBox tb)
                {
                    tb.SelectionStart = tb.Text.Length;
                    tb.SelectionLength = 0;
                }
            }));
        }
    }
}