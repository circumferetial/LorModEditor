using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Synthesis.Core.Tools;

namespace Synthesis.Feature.SkinEditor;

public partial class SkinEditorView : UserControl, IRegionMemberLifetime
{
    // 交互状态
    private Point _clickOffset;
    private bool _isDraggingBody;
    private bool _isDraggingHead;
    private bool _isPanning;

    private Point _lastMousePosition;
    // ==========================================
    // 核心常量
    // ==========================================

// 用于记录当前正在监听的对象，防止切换时内存泄漏
    private INotifyPropertyChanged? _listeningAction;

// 新增一个字段，用来存当前监听的 Skin
    private INotifyPropertyChanged? _listeningSkin;

    // 图片原始尺寸缓存

    public SkinEditorView()
    {
        InitializeComponent();

        // 初始视野居中 (大概值，防止(0,0)在左上角看不见)
        ViewTranslate.X = 400;
        ViewTranslate.Y = 300;

        Loaded += (_, _) => ApplySkinSorting();
    }

    private SkinEditorViewModel? ViewModel => DataContext as SkinEditorViewModel;
    public bool KeepAlive => true;

    private void ApplySkinSorting()
    {
        if (SkinListBox.ItemsSource == null) return;
        ViewSortHelper.ApplyModFirstNaturalSort<UnifiedSkin>(SkinListBox.ItemsSource, x => x.Name);
    }

    /// <summary>
    ///     根据 Quality 计算头部坐标使用的缩放倍率
    ///     Quality 50 -> 2.0 (原版)
    ///     Quality 100 -> 1.0 (高清)
    /// </summary>
    private double GetHeadScale()
    {
        // 如果没有选中皮肤，或者没开启 Extended 模式 -> 强制原版 2.0 倍率
        if (ViewModel?.SelectedItem == null || !ViewModel.SelectedItem.IsExtended)
        {
            return 2.0;
        }

        if (ViewModel?.SelectedAction == null) return 2.0;

        // 开启了 Extended，读取 XML 里的 Quality
        var q = ViewModel.SelectedAction.Quality;
        if (q <= 0) q = 50;

        return 100.0 / q;
    }

    private void ActionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 1. 清理旧 Action 监听 (保持之前的逻辑)
        if (_listeningAction != null)
        {
            _listeningAction.PropertyChanged -= OnActionPropertyChanged;
            _listeningAction = null;
        }

        // 2. 【新增】清理旧 Skin 监听
        if (_listeningSkin != null)
        {
            _listeningSkin.PropertyChanged -= OnSkinPropertyChanged;
            _listeningSkin = null;
        }

        if (ViewModel?.SelectedAction != null && ViewModel.SelectedItem != null)
        {
            LoadSkinImage(ViewModel.SelectedItem.Name, ViewModel.SelectedAction.ActionName);

            // 3. 监听 Action (保持之前的逻辑)
            _listeningAction = ViewModel.SelectedAction;
            _listeningAction.PropertyChanged += OnActionPropertyChanged;

            // 4. 【新增】监听 Skin (为了捕获 IsExtended 的开关变化)
            if (ViewModel.SelectedItem is INotifyPropertyChanged skinNotify)
            {
                _listeningSkin = skinNotify;
                _listeningSkin.PropertyChanged += OnSkinPropertyChanged;
            }
        }
    }

// 【新增】Skin 属性变化回调
    private void OnSkinPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 当 Extended 开关被切换时
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "IsExtended")
        {
            // 重新应用尺寸 (变回512 或者 变回大图)
            ApplyImageSize();
            // 重新计算坐标 (倍率变了)
            UpdateCanvasPositions();
        }
    }

    private void OnActionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // 如果是所有属性刷新 (null/empty)
        // 或者 涉及到尺寸变化的属性 (SizeX, SizeY, Quality)
        if (string.IsNullOrEmpty(e.PropertyName) ||
            e.PropertyName == "SizeX" ||
            e.PropertyName == "SizeY" ||
            e.PropertyName == "Quality")
        {
            // 1. 重新设置图片宽高 (重要！否则手改 SizeX 图片不会变)
            ApplyImageSize();

            // 2. 重新计算坐标 (Quality 主要影响头部坐标换算)
            UpdateCanvasPositions();

            return;// 处理完了直接返回，不用往下走了
        }

        // 仅涉及位置移动的属性
        if (e.PropertyName == "PivotX" || e.PropertyName == "PivotY" ||
            e.PropertyName == "HeadX" || e.PropertyName == "HeadY" ||
            e.PropertyName == "Direction")
        {
            UpdateCanvasPositions();
        }
    }


    private void LoadSkinImage(string skinName, string actionName)
    {
        // 1. 基础检查
        if (string.IsNullOrEmpty(skinName) || string.IsNullOrEmpty(actionName) || ViewModel?.Manager == null) return;

        // 2. 拼接路径
        var path = Path.Combine(ViewModel.Manager.ProjectRootPath!, "Resource", "CharacterSkin", skinName,
            "ClothCustom", $"{actionName}.png");

        // 3. 检查文件是否存在
        if (File.Exists(path))
        {
            try
            {
                // =========================================================
                // 【修正后的终极方案】
                // =========================================================

                // A. 先把文件一口气读进内存数组 (彻底解决文件占用)
                var imageBytes = File.ReadAllBytes(path);

                // B. 创建 BitmapImage
                var bitmap = new BitmapImage();

                bitmap.BeginInit();

                // C. 关键：必须设为 OnLoad，让它立刻从流里解码，不要延迟
                bitmap.CacheOption = BitmapCacheOption.OnLoad;

                // D. 喂给它内存流
                bitmap.StreamSource = new MemoryStream(imageBytes);

                // E. 【重要】千万不要加 CreateOptions = IgnoreImageCache，否则会报 key null 异常！
                // bitmap.CreateOptions = BitmapCreateOptions.IgnoreImageCache; // <--- 这一行是罪魁祸首，删掉！

                bitmap.EndInit();

                // F. 冻结（高性能，允许跨线程）
                bitmap.Freeze();

                // G. 赋值给 UI
                BodyImage.Source = bitmap;

                // H. 后续逻辑：应用尺寸和位置
                ApplyImageSize();
                Dispatcher.BeginInvoke(new Action(UpdateCanvasPositions), DispatcherPriority.Render);
            }
            catch (Exception ex)
            {
                // 调试用：如果还出错，这行能告诉你原因
                MessageBox.Show(ex.Message);
                BodyImage.Source = null;
            }
        }
        else
        {
            BodyImage.Source = null;
        }
    }

    private void ApplyImageSize()
    {
        if (BodyImage.Source == null || ViewModel?.SelectedAction == null || ViewModel?.SelectedItem == null) return;

        var action = ViewModel.SelectedAction;

        // 判断是否开启了 Extended
        var isExtended = ViewModel.SelectedItem.IsExtended;

        if (isExtended)
        {
            // Extended模式：完全听从 XML 的指挥
            BodyImage.Width = action.SizeX;
            BodyImage.Height = action.SizeY;
        }
        else
        {
            // 原版模式：强制 512，模拟游戏内非 Extended 的表现
            BodyImage.Width = 512;
            BodyImage.Height = 512;
        }
    }

    private void UpdateCanvasPositions()
    {
        var action = ViewModel?.SelectedAction;
        if (action == null) return;

        // 头部坐标受 quality 影响，身体 pivot 不受 quality 影响
        var headScale = GetHeadScale();

        var halfWidth = BodyImage.Width / 2.0;
        var halfHeight = BodyImage.Height / 2.0;

        // 1. 更新身体
        if (double.TryParse(action.PivotX, out var px) && double.TryParse(action.PivotY, out var py))
        {
            // 与 ExtendedLoader 对齐：screen = pivot / 2
            var screenLeft = -(px / 2.0) - halfWidth;
            var screenTop = (py / 2.0) - halfHeight;

            Canvas.SetLeft(BodyImage, screenLeft);
            Canvas.SetTop(BodyImage, screenTop);

            // 2. 更新头部
            if (double.TryParse(action.HeadX, out var hx) && double.TryParse(action.HeadY, out var hy))
            {
                var headScreenX = -hx / headScale;
                var headScreenY = -(hy / headScale);

                Canvas.SetLeft(HeadPoint, headScreenX - HeadPoint.Width / 2);
                Canvas.SetTop(HeadPoint, headScreenY - HeadPoint.Height / 2);
            }
        }
    }

    private void Viewport_MouseMove(object sender, MouseEventArgs e)
    {
        // ==========================================
        // 1. 补回丢失的视野平移逻辑 (解决警告)
        // ==========================================
        if (_isPanning)
        {
            var currentPos = e.GetPosition(ViewportCanvas);
            // 计算鼠标移动距离
            var deltaX = currentPos.X - _lastMousePosition.X;
            var deltaY = currentPos.Y - _lastMousePosition.Y;

            // 应用到变换对象 (ViewTranslate 是 XAML 里定义的 TranslateTransform)
            ViewTranslate.X += deltaX;
            ViewTranslate.Y += deltaY;

            // 更新最后位置
            _lastMousePosition = currentPos;
            return;// 平移时不要触发拖拽
        }

        // ==========================================
        // 下面是之前的拖拽逻辑 (包含X轴反转修正)
        // ==========================================
        if (ViewModel?.SelectedAction == null) return;
        if (!_isDraggingBody && !_isDraggingHead) return;

        var action = ViewModel.SelectedAction;
        var mouseInWorld = e.GetPosition(WorldCanvas);
        var headScale = GetHeadScale();
        if (_isDraggingBody)
        {
            var newLeft = mouseInWorld.X - _clickOffset.X;
            var newTop = mouseInWorld.Y - _clickOffset.Y;

            Canvas.SetLeft(BodyImage, newLeft);
            Canvas.SetTop(BodyImage, newTop);

            var halfWidth = BodyImage.Width / 2.0;
            var halfHeight = BodyImage.Height / 2.0;


            var gamePivotX = -2.0 * (halfWidth + newLeft);
            var gamePivotY = 2.0 * (halfHeight + newTop);

            action.PivotX = gamePivotX.ToString("F1");
            action.PivotY = gamePivotY.ToString("F1");

            UpdateCanvasPositions();
        }
        else if (_isDraggingHead)
        {
            var newLeft = mouseInWorld.X - _clickOffset.X;
            var newTop = mouseInWorld.Y - _clickOffset.Y;

            Canvas.SetLeft(HeadPoint, newLeft);
            Canvas.SetTop(HeadPoint, newTop);

            var headCenterX = newLeft + HeadPoint.Width / 2;
            var headCenterY = newTop + HeadPoint.Height / 2;

            // X轴反转修正
            var gameHeadX = -(headCenterX * headScale);
            var gameHeadY = -(headCenterY * headScale);

            action.HeadX = gameHeadX.ToString("F1");
            action.HeadY = gameHeadY.ToString("F1");
        }
    }

    // ==========================================
    // 基础交互事件 (滚轮、点击等)
    // ==========================================
    private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
        if (ViewScale.ScaleX * zoomFactor < 0.1 || ViewScale.ScaleX * zoomFactor > 10.0) return;
        ViewScale.ScaleX *= zoomFactor;
        ViewScale.ScaleY *= zoomFactor;
    }

    private void Viewport_MouseRightDown(object sender, MouseButtonEventArgs e)
    {
        _isPanning = true;
        _lastMousePosition = e.GetPosition(ViewportCanvas);
        ViewportCanvas.CaptureMouse();
        ViewportCanvas.Cursor = Cursors.ScrollAll;
    }

    private void Viewport_MouseRightUp(object sender, MouseButtonEventArgs e)
    {
        _isPanning = false;
        ViewportCanvas.ReleaseMouseCapture();
        ViewportCanvas.Cursor = Cursors.Arrow;
    }

    private void Body_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingBody = true;
        _clickOffset = e.GetPosition(BodyImage);
        BodyImage.CaptureMouse();
        e.Handled = true;
    }

    private void Body_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingBody = false;
        BodyImage.ReleaseMouseCapture();
    }

    private void Head_MouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDraggingHead = true;
        _clickOffset = e.GetPosition(HeadPoint);
        HeadPoint.CaptureMouse();
        e.Handled = true;
    }

    private void Head_MouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDraggingHead = false;
        HeadPoint.ReleaseMouseCapture();
    }
}
