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
        if (ViewModel?.SelectedAction != null && ViewModel.SelectedItem != null)
        {
            LoadSkinImage(ViewModel.SelectedItem.Name, ViewModel.SelectedAction.ActionName);
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

    // ==========================================
    // 读取数据: XML -> 屏幕
    // ==========================================
    private void UpdateCanvasPositions()
    {
        var action = ViewModel?.SelectedAction;
        if (action == null) return;

        // 【通用公式读取】
        // 游戏Pivot是中心(0.5)，所以偏移基准是 Height/2
        double halfWidth = BodyImage.Width / 2.0;
        double halfHeight = BodyImage.Height / 2.0;

        // 1. 更新身体
        if (double.TryParse(action.PivotX, out var px) && double.TryParse(action.PivotY, out var py))
        {
            // 游戏坐标 / 2 = 屏幕逻辑偏移
            double logicOffsetX = px / GameScale;
            double logicOffsetY = py / GameScale;

            // 屏幕位置 = 偏移基准 + 逻辑偏移
            // 注意 X: (256 + off) - 半宽 = (半宽 + off) - 半宽 = off ... 
            // 让我们回溯公式：PivotX = (半宽 + Left) * 2
            // Left = (PivotX / 2) - 半宽
            
            double screenLeft = (px / GameScale) - halfWidth;
            
            // 注意 Y: PivotY = (半高 + Top) * 2
            // Top = (PivotY / 2) - 半高
            double screenTop = (py / GameScale) - halfHeight;

            Canvas.SetLeft(BodyImage, screenLeft);
            Canvas.SetTop(BodyImage, screenTop);

            // 2. 更新头部 (红点)
            if (double.TryParse(action.HeadX, out var hx) && double.TryParse(action.HeadY, out var hy))
            {
                // Head 是相对脚底(绿线)的偏移
                // 游戏里 HeadY 向上为正，屏幕向下为正，需要取反
                
                double headScreenX = hx / GameScale;
                double headScreenY = -(hy / GameScale); 
                
                // 绿线在 (0,0)，直接画
                Canvas.SetLeft(HeadPoint, headScreenX - HeadPoint.Width / 2);
                Canvas.SetTop(HeadPoint, headScreenY - HeadPoint.Height / 2);
            }
        }
    }

    // ==========================================
    // 鼠标移动逻辑 (核心)
    // ==========================================
    private void Viewport_MouseMove(object sender, MouseEventArgs e)
    {
        // --- 1. 视野平移 ---
        if (_isPanning)
        {
            var currentPos = e.GetPosition(ViewportCanvas);
            var deltaX = currentPos.X - _lastMousePosition.X;
            var deltaY = currentPos.Y - _lastMousePosition.Y;
            ViewTranslate.X += deltaX;
            ViewTranslate.Y += deltaY;
            _lastMousePosition = currentPos;
            return;
        }

        if (ViewModel?.SelectedAction == null) return;
        if (!_isDraggingBody && !_isDraggingHead) return;

        var action = ViewModel.SelectedAction;
        var mouseInWorld = e.GetPosition(WorldCanvas);

        // --- 2. 拖拽身体 ---
        if (_isDraggingBody)
        {
            double newLeft = mouseInWorld.X - _clickOffset.X;
            double newTop = mouseInWorld.Y - _clickOffset.Y;

            Canvas.SetLeft(BodyImage, newLeft);
            Canvas.SetTop(BodyImage, newTop);

            // 【通用公式保存】
            // 无论尺寸是多少，半高 (HalfHeight) 永远对应游戏里的中心点
            double halfWidth = BodyImage.Width / 2.0;
            double halfHeight = BodyImage.Height / 2.0;

            // 公式：(基准点 + 屏幕偏移) * 2
            double gamePivotX = (halfWidth + newLeft) * GameScale;
            double gamePivotY = (halfHeight + newTop) * GameScale;

            action.PivotX = gamePivotX.ToString("F1");
            action.PivotY = gamePivotY.ToString("F1");

            UpdateCanvasPositions(); // 刷新头部跟随
        }
        // --- 3. 拖拽头部 ---
        else if (_isDraggingHead)
        {
            double newLeft = mouseInWorld.X - _clickOffset.X;
            double newTop = mouseInWorld.Y - _clickOffset.Y;

            Canvas.SetLeft(HeadPoint, newLeft);
            Canvas.SetTop(HeadPoint, newTop);

            // 头部反算：直接算距离绿线(0,0)的距离
            // 中心点 = 左上角 + 半径
            double headCenterX = newLeft + HeadPoint.Width / 2;
            double headCenterY = newTop + HeadPoint.Height / 2;

            // 转为游戏坐标
            // X直接乘
            // Y要取反 (屏幕向下正 -> 游戏向上正)
            double gameHeadX = headCenterX * GameScale;
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

