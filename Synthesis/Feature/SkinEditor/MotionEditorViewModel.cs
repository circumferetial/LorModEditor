using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using JetBrains.Annotations;
using Synthesis.Core;
using Synthesis.Core.Tools;

// 引用 SkinRepository

namespace Synthesis.Feature.SkinEditor;// 命名空间要注意


public class MotionEditorViewModel(ProjectManager projectManager) : BindableBase, INavigationAware
{
    // --- 1. 左侧列表数据 ---
    public ObservableCollection<SkinEntry> Skins => projectManager.NewSkinRepo.Items;


    public SkinEntry? SelectedSkin
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                RefreshMotions();
            }
        }
    }

    // --- 2. 中间动作列表 ---
    public ObservableCollection<CharacterMotionData> MotionList { get; } = new();

    public CharacterMotionData? CurrentMotion
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                LoadMotionImage();
            }
        }
    }

    // --- 3. 编辑区显示数据 ---
    public ImageSource? PreviewImage
    {
        get;
        set => SetProperty(ref field, value);
    }

    private double _currentImageHeight = 512;
    private double _currentImageWidth = 512;

    // --- 核心：视觉偏移量 (Visual Offset) 绑定 ---
    // 这里的逻辑：UI 操作 VisualOffset -> 自动根据 Quality 算出 XML 的 Pivot
        
    public double VisualOffsetY
    {
        get
        {
            if (CurrentMotion == null) return 0;
            double factor = 100.0 / Math.Max(1, CurrentMotion.Quality);
            // 反向公式：(XML值 / 倍率) - (图片高 / 2)
            return (CurrentMotion.PivotY / factor) - (_currentImageHeight / 2.0);
        }
        set
        {
            if (CurrentMotion == null) return;
            // 触发界面刷新
            RaisePropertyChanged();
                
            // 正向公式：(图片高 / 2 + 偏移) * 倍率
            double factor = 100.0 / Math.Max(1, CurrentMotion.Quality);
            CurrentMotion.PivotY = (_currentImageHeight / 2.0 + value) * factor;
        }
    }

    public double VisualOffsetX
    {
        get
        {
            if (CurrentMotion == null) return 0;
            double factor = 100.0 / Math.Max(1, CurrentMotion.Quality);
            return (CurrentMotion.PivotX / factor) - (CurrentMotion.SizeX / 2.0);
        }
        set
        {
            if (CurrentMotion == null) return;
            RaisePropertyChanged();
            double factor = 100.0 / Math.Max(1, CurrentMotion.Quality);
            CurrentMotion.PivotX = (CurrentMotion.SizeX / 2.0 + value) * factor;
        }
    }

    // --- 质量改变时的逻辑 ---
    // 当用户拖动 Quality 滑块，XML 变了，但我们需要强制刷新 VisualOffset 的 getter
    // 实际上，我们更希望位置不变，XML Pivot 自动变。
    // 为了简化，这里我们只做 Notification，让用户自己调。
    // 或者：监听 CurrentMotion 的 PropertyChanged (像之前的版本那样)

    // --- 辅助方法 ---

    private void RefreshMotions()
    {
        MotionList.Clear();
        if (SelectedSkin == null) return;

        var motions = SelectedSkin.GetAllMotions();
        foreach (var m in motions) MotionList.Add(m);

        // 默认选中第一个 (通常是 Default)
        CurrentMotion = MotionList.FirstOrDefault();
    }

    private void LoadMotionImage()
    {
    
        if (SelectedSkin == null || CurrentMotion == null) return;
        // 监听当前动作的数据变化，以便实时刷新界面
        // 移除旧的监听防止内存泄漏 (简化版略过，实际项目要注意)
        CurrentMotion.PropertyChanged -= CurrentMotion_PropertyChanged;
        CurrentMotion.PropertyChanged += CurrentMotion_PropertyChanged;

        string imagePath = SelectedSkin.GetImagePath(CurrentMotion.MotionName);

        if (File.Exists(imagePath))
        {
            var bitmap = ImageHelper.LoadBitmapWithoutLocking(imagePath);
            PreviewImage = bitmap;
            if (bitmap != null)
            {
                _currentImageHeight = bitmap.PixelHeight;
                _currentImageWidth = bitmap.PixelWidth;
            }

            // 自动把图片尺寸写入 XML (如果还没写的话，或者强制更新)
            CurrentMotion.SizeX = _currentImageWidth;
            CurrentMotion.SizeY = _currentImageHeight;
        }
        else
        {
            PreviewImage = null;
            _currentImageHeight = CurrentMotion.SizeY > 0 ? CurrentMotion.SizeY : 512;
        }

        // 刷新所有属性
        RaisePropertyChanged(nameof(VisualOffsetY));
        RaisePropertyChanged(nameof(VisualOffsetX));
        RaisePropertyChanged(nameof(PreviewImage));
    }

    private void CurrentMotion_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CharacterMotionData.Quality))
        {
            // 如果质量变了，视觉偏移量也得变（或者 XML Pivot 变），这里简单让界面刷新
            RaisePropertyChanged(nameof(VisualOffsetY));
            RaisePropertyChanged(nameof(VisualOffsetX));
        }
    }

    // --- 导航接口 ---
    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        SelectedSkin ??= Skins.FirstOrDefault();
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;
    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}