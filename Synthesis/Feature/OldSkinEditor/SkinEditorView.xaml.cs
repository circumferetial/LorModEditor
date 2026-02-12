using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Synthesis.Feature.OldSkinEditor;

public partial class SkinEditorView : UserControl, IRegionMemberLifetime
{
    // ==========================================
    // 核心常量
    // ==========================================
    private const double GameScale = 2.0; // PPU 50 导致的 2倍缩放
// 用于记录当前正在监听的对象，防止切换时内存泄漏
    private System.ComponentModel.INotifyPropertyChanged? _listeningAction;
    // 交互状态
    private Point _clickOffset;
    private Point _lastMousePosition;
    private bool _isPanning;
    private bool _isDraggingBody;
    private bool _isDraggingHead;

    // 图片原始尺寸缓存
    private double _originalWidth;
    private double _originalHeight;

    public SkinEditorView()
    {
        InitializeComponent();
        
        // 初始视野居中 (大概值，防止(0,0)在左上角看不见)
        ViewTranslate.X = 400; 
        ViewTranslate.Y = 300;
    }

    private SkinEditorViewModel? ViewModel => DataContext as SkinEditorViewModel;
    public bool KeepAlive => true;

    // ==========================================
    // 列表选择变更
    // ==========================================
    private void ActionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // ==============================================================
        // 1. 清理旧监听 (非常重要，否则切换到B时，改A的数值还会触发B的刷新)
        // ==============================================================
        if (_listeningAction != null)
        {
            _listeningAction.PropertyChanged -= OnActionPropertyChanged;
            _listeningAction = null;
        }

        if (ViewModel?.SelectedAction != null && ViewModel.SelectedItem != null)
        {
            // 2. 加载图片 (原有逻辑)
            LoadSkinImage(ViewModel.SelectedItem.Name, ViewModel.SelectedAction.ActionName);

            // 3. 【新增】订阅属性变化事件
            // 因为 UnifiedSkinAction 继承自 XWrapper，而 XWrapper 实现了 INotifyPropertyChanged
            _listeningAction = ViewModel.SelectedAction;
            _listeningAction.PropertyChanged += OnActionPropertyChanged;
        }
    }
    
    private void OnActionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // 你的 XWrapper.SetAttr 此时发送的 e.PropertyName 是 null (代表所有属性刷新)
        // 或者将来你优化后可能会发送具体的 "PivotX"
    
        // 只要属性名为空(全部更新) 或者 是我们要关注的坐标字段，就刷新画布
        if (string.IsNullOrEmpty(e.PropertyName) || 
            e.PropertyName == "PivotX" || e.PropertyName == "PivotY" || 
            e.PropertyName == "HeadX" || e.PropertyName == "HeadY" || 
            e.PropertyName == "Direction") // Direction 改变可能不需要刷坐标，但防万一
        {
            // 确保在 UI 线程执行
            UpdateCanvasPositions();
        }
    }

    // ==========================================
    // HD 开关切换
    // ==========================================
    private void HDMode_Changed(object sender, RoutedEventArgs e)
    {
        ApplyImageSize(); // 重新应用尺寸
        UpdateCanvasPositions(); // 重新计算位置，因为 Height 变了
    }

    // ==========================================
    // 加载图片
    // ==========================================
    private void LoadSkinImage(string skinName, string actionName)
    {
        if (string.IsNullOrEmpty(skinName) || string.IsNullOrEmpty(actionName) || ViewModel?.Manager == null) return;

        var path = Path.Combine(ViewModel.Manager.ProjectRootPath!, "Resource", "CharacterSkin", skinName,
            "ClothCustom", $"{actionName}.png");

        if (File.Exists(path))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                BodyImage.Source = bitmap;
                
                // 记录原始尺寸
                _originalWidth = bitmap.PixelWidth;
                _originalHeight = bitmap.PixelHeight;

                // 应用尺寸 (根据是否开启 HD)
                ApplyImageSize();

                // 强制刷新UI
                Dispatcher.BeginInvoke(new Action(UpdateCanvasPositions), DispatcherPriority.Render);
            }
            catch { BodyImage.Source = null; }
        }
        else
        {
            BodyImage.Source = null;
        }
    }

    // ==========================================
    // 应用图片尺寸策略
    // ==========================================
    private void ApplyImageSize()
    {
        if (BodyImage.Source == null) return;

        bool isHd = ViewModel?.IsHDMode ?? false;

        if (isHd)
        {
            // HD模式：原图尺寸
            BodyImage.Width = _originalWidth;
            BodyImage.Height = _originalHeight;
        }
        else
        {
            // 原版模式：强制挤压成 512x512
            // 无论原图多大，都变成这个尺寸，模拟游戏原版行为
            BodyImage.Width = 512;
            BodyImage.Height = 512;
        }
    }

    private void UpdateCanvasPositions()
    {
        var action = ViewModel?.SelectedAction;
        if (action == null) return;

        double halfWidth = BodyImage.Width / 2.0;
        double halfHeight = BodyImage.Height / 2.0;

        // 1. 更新身体
        if (double.TryParse(action.PivotX, out var px) && double.TryParse(action.PivotY, out var py))
        {
            // 【修改点 1】: 读取 PivotX 时取反 (-px)
            // 原逻辑: double screenLeft = (px / GameScale) - halfWidth;
            double screenLeft = (-px / GameScale) - halfWidth; 
        
            double screenTop = (py / GameScale) - halfHeight;

            Canvas.SetLeft(BodyImage, screenLeft);
            Canvas.SetTop(BodyImage, screenTop);

            // 2. 更新头部 (红点)
            if (double.TryParse(action.HeadX, out var hx) && double.TryParse(action.HeadY, out var hy))
            {
                // 【修改点 2】: 读取 HeadX 时取反 (-hx)
                // 原逻辑: double headScreenX = hx / GameScale;
                double headScreenX = -hx / GameScale;
            
                // Y轴保持原有的取反逻辑 (因为之前代码里HeadY本来就是反的)
                double headScreenY = -(hy / GameScale); 
            
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
        return; // 平移时不要触发拖拽
    }

    // ==========================================
    // 下面是之前的拖拽逻辑 (包含X轴反转修正)
    // ==========================================
    if (ViewModel?.SelectedAction == null) return;
    if (!_isDraggingBody && !_isDraggingHead) return;

    var action = ViewModel.SelectedAction;
    var mouseInWorld = e.GetPosition(WorldCanvas);

    if (_isDraggingBody)
    {
        double newLeft = mouseInWorld.X - _clickOffset.X;
        double newTop = mouseInWorld.Y - _clickOffset.Y;

        Canvas.SetLeft(BodyImage, newLeft);
        Canvas.SetTop(BodyImage, newTop);

        double halfWidth = BodyImage.Width / 2.0;
        double halfHeight = BodyImage.Height / 2.0;

        // X轴反转修正
        double gamePivotX = -(halfWidth + newLeft) * GameScale;
        double gamePivotY = (halfHeight + newTop) * GameScale;

        action.PivotX = gamePivotX.ToString("F1");
        action.PivotY = gamePivotY.ToString("F1");

        UpdateCanvasPositions();
    }
    else if (_isDraggingHead)
    {
        double newLeft = mouseInWorld.X - _clickOffset.X;
        double newTop = mouseInWorld.Y - _clickOffset.Y;

        Canvas.SetLeft(HeadPoint, newLeft);
        Canvas.SetTop(HeadPoint, newTop);

        double headCenterX = newLeft + HeadPoint.Width / 2;
        double headCenterY = newTop + HeadPoint.Height / 2;

        // X轴反转修正
        double gameHeadX = -(headCenterX * GameScale);
        double gameHeadY = -(headCenterY * GameScale);

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

