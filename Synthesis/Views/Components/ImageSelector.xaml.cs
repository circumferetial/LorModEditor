using System.Collections;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Synthesis.Core;

namespace Synthesis.Views.Components;

public partial class ImageSelector : UserControl
{
    private const int MaxCachedImages = 128;
    private static readonly Dictionary<string, BitmapSource> BitmapCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Lock BitmapCacheLock = new();

    public static readonly DependencyProperty ManagerProperty =
        DependencyProperty.Register(
            nameof(Manager),
            typeof(ProjectManager),
            typeof(ImageSelector),
            new PropertyMetadata(null, OnManagerChanged));

    public static readonly DependencyProperty SelectedImageNameProperty =
        DependencyProperty.Register(
            nameof(SelectedImageName),
            typeof(string),
            typeof(ImageSelector),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnImageNameChanged));

    public static readonly DependencyProperty ImageListProperty =
        DependencyProperty.Register(nameof(ImageList), typeof(IEnumerable), typeof(ImageSelector));

    public static readonly DependencyProperty LabelTextProperty =
        DependencyProperty.Register(nameof(LabelText), typeof(string), typeof(ImageSelector),
            new PropertyMetadata("Image:"));

    private string? _lastLoadedPath;
    private CancellationTokenSource? _loadImageCts;

    public ImageSelector()
    {
        InitializeComponent();
        Unloaded += (_, _) => _loadImageCts?.Cancel();
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

    public double PreviewWidth { get; set; } = 200.0;
    public double PreviewHeight { get; set; } = 150.0;

    private static void OnManagerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (ImageSelector)d;
        view.LoadImage(view.SelectedImageName);
    }

    private static void OnImageNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ImageSelector)d).LoadImage(e.NewValue as string);
    }

    private async void LoadImage(string? name)
    {
        if (string.IsNullOrEmpty(name) || Manager == null)
        {
            _loadImageCts?.Cancel();
            PreviewImage.Source = null;
            _lastLoadedPath = null;
            return;
        }

        try
        {
            var artworkPath = Manager.GetArtworkPath(name);
            if (artworkPath != null && File.Exists(artworkPath))
            {
                if (string.Equals(_lastLoadedPath, artworkPath, StringComparison.OrdinalIgnoreCase) &&
                    PreviewImage.Source != null)
                {
                    return;
                }

                _lastLoadedPath = artworkPath;
                _loadImageCts?.Cancel();
                var loadCts = new CancellationTokenSource();
                _loadImageCts = loadCts;

                var bitmap = await Task.Run(() => GetOrLoadBitmap(artworkPath), loadCts.Token);
                if (loadCts.IsCancellationRequested ||
                    !string.Equals(_lastLoadedPath, artworkPath, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                PreviewImage.Source = bitmap;
            }
            else
            {
                _loadImageCts?.Cancel();
                PreviewImage.Source = null;
                _lastLoadedPath = null;
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            PreviewImage.Source = null;
            _lastLoadedPath = null;
        }
    }

    private static BitmapSource GetOrLoadBitmap(string artworkPath)
    {
        using (BitmapCacheLock.EnterScope())
        {
            if (BitmapCache.TryGetValue(artworkPath, out var bitmapSource))
            {
                return bitmapSource;
            }
        }

        var bitmap = new BitmapImage();
        bitmap.BeginInit();
        bitmap.UriSource = new Uri(artworkPath);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();

        using (BitmapCacheLock.EnterScope())
        {
            if (!BitmapCache.ContainsKey(artworkPath))
            {
                if (BitmapCache.Count >= MaxCachedImages)
                {
                    var firstKey = BitmapCache.Keys.FirstOrDefault();
                    if (!string.IsNullOrEmpty(firstKey))
                    {
                        BitmapCache.Remove(firstKey);
                    }
                }

                BitmapCache[artworkPath] = bitmap;
            }

            return BitmapCache[artworkPath];
        }
    }

    private void ImageCombo_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (e.OriginalSource is not TextBox input)
        {
            return;
        }

        if (ImageCombo.ItemsSource != null)
        {
            var view = CollectionViewSource.GetDefaultView(ImageCombo.ItemsSource);
            if (view != null)
            {
                view.Filter = obj =>
                {
                    if (string.IsNullOrEmpty(input.Text))
                    {
                        return true;
                    }

                    return obj is string text && text.Contains(input.Text, StringComparison.OrdinalIgnoreCase);
                };
            }
        }

        if (ImageCombo.IsKeyboardFocusWithin && !ImageCombo.IsDropDownOpen)
        {
            ImageCombo.IsDropDownOpen = true;
        }

        LoadImage(input.Text);
    }

    private void ImageCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ImageCombo.SelectedItem is not string selectedName)
        {
            return;
        }

        LoadImage(selectedName);
        Dispatcher.BeginInvoke((Action)(() =>
        {
            if (ImageCombo.Template.FindName("PART_EditableTextBox", ImageCombo) is TextBox editableTextBox)
            {
                editableTextBox.SelectionStart = editableTextBox.Text.Length;
                editableTextBox.SelectionLength = 0;
            }
        }), DispatcherPriority.Normal);
    }
}
