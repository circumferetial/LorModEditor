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
    private Point _clickOffset;
    private bool _isDraggingBody;
    private bool _isDraggingHead;
    private bool _isPanning;
    private Point _lastMousePosition;
    private INotifyPropertyChanged? _listeningAction;
    private INotifyPropertyChanged? _listeningSkin;

    public SkinEditorView()
    {
        InitializeComponent();
        ViewTranslate.X = 400.0;
        ViewTranslate.Y = 300.0;
        Loaded += (_, _) => ApplySkinSorting();
    }

    private SkinEditorViewModel? ViewModel => DataContext as SkinEditorViewModel;

    public bool KeepAlive => true;

    private void ApplySkinSorting()
    {
        if (SkinListBox.ItemsSource != null)
        {
            ViewSortHelper.ApplyModFirstNaturalSort<UnifiedSkin>(SkinListBox.ItemsSource, x => x.Name);
        }
    }

    private double GetHeadScale()
    {
        if (ViewModel?.SelectedItem == null || !ViewModel.SelectedItem.IsExtended)
        {
            return 2.0;
        }

        if (ViewModel.SelectedAction == null)
        {
            return 2.0;
        }

        var quality = ViewModel.SelectedAction.Quality;
        if (quality <= 0)
        {
            quality = 50;
        }

        return 100.0 / quality;
    }

    private void ActionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_listeningAction != null)
        {
            _listeningAction.PropertyChanged -= OnActionPropertyChanged;
            _listeningAction = null;
        }

        if (_listeningSkin != null)
        {
            _listeningSkin.PropertyChanged -= OnSkinPropertyChanged;
            _listeningSkin = null;
        }

        if (ViewModel?.SelectedAction == null || ViewModel.SelectedItem == null)
        {
            return;
        }

        LoadSkinImage(ViewModel.SelectedItem.Name, ViewModel.SelectedAction.ActionName);
        _listeningAction = ViewModel.SelectedAction;
        _listeningAction.PropertyChanged += OnActionPropertyChanged;
        _listeningSkin = ViewModel.SelectedItem;
        _listeningSkin.PropertyChanged += OnSkinPropertyChanged;
    }

    private void OnSkinPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(UnifiedSkin.IsExtended))
        {
            ApplyImageSize();
            UpdateCanvasPositions();
        }
    }

    private void OnActionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) ||
            e.PropertyName == nameof(UnifiedSkinAction.SizeX) ||
            e.PropertyName == nameof(UnifiedSkinAction.SizeY) ||
            e.PropertyName == nameof(UnifiedSkinAction.Quality))
        {
            ApplyImageSize();
            UpdateCanvasPositions();
        }
        else if (e.PropertyName == nameof(UnifiedSkinAction.PivotX) ||
                 e.PropertyName == nameof(UnifiedSkinAction.PivotY) ||
                 e.PropertyName == nameof(UnifiedSkinAction.HeadX) ||
                 e.PropertyName == nameof(UnifiedSkinAction.HeadY) ||
                 e.PropertyName == nameof(UnifiedSkinAction.Direction))
        {
            UpdateCanvasPositions();
        }
    }

    private void LoadSkinImage(string skinName, string actionName)
    {
        if (string.IsNullOrEmpty(skinName) || string.IsNullOrEmpty(actionName) || ViewModel?.Manager == null)
        {
            return;
        }

        var imagePath = Path.Combine(
            ViewModel.Manager.ProjectRootPath!,
            "Resource",
            "CharacterSkin",
            skinName,
            "ClothCustom",
            $"{actionName}.png");

        if (!File.Exists(imagePath))
        {
            BodyImage.Source = null;
            return;
        }

        try
        {
            var buffer = File.ReadAllBytes(imagePath);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = new MemoryStream(buffer);
            bitmap.EndInit();
            bitmap.Freeze();
            BodyImage.Source = bitmap;
            ApplyImageSize();
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(UpdateCanvasPositions));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            BodyImage.Source = null;
        }
    }

    private void ApplyImageSize()
    {
        if (BodyImage.Source == null || ViewModel?.SelectedAction == null || ViewModel.SelectedItem == null)
        {
            return;
        }

        var action = ViewModel.SelectedAction;
        if (ViewModel.SelectedItem.IsExtended)
        {
            BodyImage.Width = action.SizeX;
            BodyImage.Height = action.SizeY;
        }
        else
        {
            BodyImage.Width = 512.0;
            BodyImage.Height = 512.0;
        }
    }

    private void UpdateCanvasPositions()
    {
        var action = ViewModel?.SelectedAction;
        if (action == null)
        {
            return;
        }

        var headScale = GetHeadScale();
        var halfBodyWidth = BodyImage.Width / 2.0;
        var halfBodyHeight = BodyImage.Height / 2.0;

        if (double.TryParse(action.PivotX, out var pivotX) &&
            double.TryParse(action.PivotY, out var pivotY))
        {
            var bodyLeft = -pivotX / 2.0 - halfBodyWidth;
            var bodyTop = pivotY / 2.0 - halfBodyHeight;
            Canvas.SetLeft(BodyImage, bodyLeft);
            Canvas.SetTop(BodyImage, bodyTop);

            if (double.TryParse(action.HeadX, out var headX) &&
                double.TryParse(action.HeadY, out var headY))
            {
                var headCenterX = -headX / headScale;
                var headCenterY = -headY / headScale;
                Canvas.SetLeft(HeadPoint, headCenterX - HeadPoint.Width / 2.0);
                Canvas.SetTop(HeadPoint, headCenterY - HeadPoint.Height / 2.0);
            }
        }
    }

    private void Viewport_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isPanning)
        {
            var position = e.GetPosition(ViewportCanvas);
            ViewTranslate.X += position.X - _lastMousePosition.X;
            ViewTranslate.Y += position.Y - _lastMousePosition.Y;
            _lastMousePosition = position;
            return;
        }

        if (ViewModel?.SelectedAction == null || !_isDraggingBody && !_isDraggingHead)
        {
            return;
        }

        var action = ViewModel.SelectedAction;
        var positionOnWorld = e.GetPosition(WorldCanvas);
        var headScale = GetHeadScale();

        if (_isDraggingBody)
        {
            var bodyLeft = positionOnWorld.X - _clickOffset.X;
            var bodyTop = positionOnWorld.Y - _clickOffset.Y;
            Canvas.SetLeft(BodyImage, bodyLeft);
            Canvas.SetTop(BodyImage, bodyTop);

            var halfBodyWidth = BodyImage.Width / 2.0;
            var halfBodyHeight = BodyImage.Height / 2.0;
            action.PivotX = (-2.0 * (halfBodyWidth + bodyLeft)).ToString("F1");
            action.PivotY = (2.0 * (halfBodyHeight + bodyTop)).ToString("F1");
            UpdateCanvasPositions();
        }
        else if (_isDraggingHead)
        {
            var headLeft = positionOnWorld.X - _clickOffset.X;
            var headTop = positionOnWorld.Y - _clickOffset.Y;
            Canvas.SetLeft(HeadPoint, headLeft);
            Canvas.SetTop(HeadPoint, headTop);

            var headCenterX = headLeft + HeadPoint.Width / 2.0;
            var headCenterY = headTop + HeadPoint.Height / 2.0;
            action.HeadX = (-headCenterX * headScale).ToString("F1");
            action.HeadY = (-headCenterY * headScale).ToString("F1");
        }
    }

    private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var scaleStep = e.Delta > 0 ? 1.1 : 0.9;
        var targetScale = ViewScale.ScaleX * scaleStep;
        if (targetScale is < 0.1 or > 10.0)
        {
            return;
        }

        ViewScale.ScaleX = targetScale;
        ViewScale.ScaleY = targetScale;
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
