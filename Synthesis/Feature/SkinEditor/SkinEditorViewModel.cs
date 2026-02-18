using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Synthesis.Core;
using Synthesis.Core.Enums;

namespace Synthesis.Feature.SkinEditor;

public class SkinEditorViewModel : BindableBase
{
    private UnifiedSkin? _subscribedSkin;

    public SkinEditorViewModel(ProjectManager manager)
    {
        Manager = manager;
        CreateCommand = new DelegateCommand(Create);
        AddPresetActionCommand = new DelegateCommand(AddPresetAction, CanAddPresetAction)
            .ObservesProperty(() => SelectedItem).ObservesProperty(() => SelectedPresetAction);
        DeleteActionCommand = new DelegateCommand(DeleteAction, CanDeleteAction).ObservesProperty(() => SelectedItem)
            .ObservesProperty(() => SelectedAction);
        DeleteSkinCommand = new DelegateCommand(DeleteSkin, CanDeleteSkin).ObservesProperty(() => SelectedItem);
        ImportImageCommand = new DelegateCommand(ImportImage, () => SelectedAction != null && SelectedItem != null)
            .ObservesProperty(() => SelectedAction).ObservesProperty(() => SelectedItem);
        SetQualityCommand = new DelegateCommand<string>(SetQuality);
        AddCustomActionCommand =
            new DelegateCommand(AddCustomAction, CanEditSelectedSkin).ObservesProperty(() => SelectedItem);
        MirrorImageCommand = new DelegateCommand(MirrorImage, () => SelectedAction != null && SelectedItem != null)
            .ObservesProperty(() => SelectedAction).ObservesProperty(() => SelectedItem);
        RefreshPublishCompatibility();
    }

    public DelegateCommand MirrorImageCommand { get; }

    public ProjectManager Manager { get; }

    public IReadOnlyList<ActionDetail> PresetActions { get; } = SkinCompatibilityGuard.PresetActions;

    public UnifiedSkin? SelectedItem
    {
        get;
        set
        {
            var unifiedSkin = field;
            if (SetProperty(ref field, value))
            {
                if (unifiedSkin != null)
                {
                    DetachSkinSubscriptions(unifiedSkin);
                }
                if (value != null)
                {
                    AttachSkinSubscriptions(value);
                }
                SelectedAction = null;
                RefreshPublishCompatibility();
            }
        }
    }

    public UnifiedSkinAction? SelectedAction
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ActionDetail? SelectedPresetAction
    {
        get;
        set => SetProperty(ref field, value);
    } = ActionDetail.Default;

    public string PublishCompatibilityText
    {
        get;
        private set => SetProperty(ref field, value);
    } = "未选择皮肤";

    public bool IsPublishCompatible
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public DelegateCommand CreateCommand { get; }

    public DelegateCommand AddPresetActionCommand { get; }

    public DelegateCommand AddCustomActionCommand { get; }

    public DelegateCommand DeleteActionCommand { get; }

    public DelegateCommand DeleteSkinCommand { get; }

    public DelegateCommand ImportImageCommand { get; }

    public DelegateCommand<string> SetQualityCommand { get; }

    private bool CanAddPresetAction()
    {
        if (SelectedItem != null && !SelectedItem.IsVanilla)
        {
            return SelectedPresetAction.HasValue;
        }
        return false;
    }

    private bool CanEditSelectedSkin()
    {
        if (SelectedItem != null)
        {
            return !SelectedItem.IsVanilla;
        }
        return false;
    }

    private bool CanDeleteAction()
    {
        if (SelectedItem != null && !SelectedItem.IsVanilla)
        {
            return SelectedAction != null;
        }
        return false;
    }

    private bool CanDeleteSkin()
    {
        if (SelectedItem != null)
        {
            return !SelectedItem.IsVanilla;
        }
        return false;
    }

    private void AttachSkinSubscriptions(UnifiedSkin skin)
    {
        _subscribedSkin = skin;
        _subscribedSkin.PropertyChanged += OnSubscribedSkinPropertyChanged;
        _subscribedSkin.Actions.CollectionChanged += OnSubscribedSkinActionsChanged;
    }

    private void DetachSkinSubscriptions(UnifiedSkin skin)
    {
        skin.PropertyChanged -= OnSubscribedSkinPropertyChanged;
        skin.Actions.CollectionChanged -= OnSubscribedSkinActionsChanged;
        if (_subscribedSkin == skin)
        {
            _subscribedSkin = null;
        }
    }

    private void OnSubscribedSkinPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == "Name")
        {
            RefreshPublishCompatibility();
        }
    }

    private void OnSubscribedSkinActionsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshPublishCompatibility();
    }

    private void RefreshPublishCompatibility()
    {
        if (SelectedItem == null)
        {
            IsPublishCompatible = false;
            PublishCompatibilityText = "未选择皮肤";
            return;
        }
        if (SelectedItem.IsVanilla)
        {
            IsPublishCompatible = true;
            PublishCompatibilityText = "原版皮肤（只读）";
            return;
        }
        var source = SkinCompatibilityGuard.Validate(SelectedItem.Element);
        var skinCompatibilityIssue = source.FirstOrDefault(x => x.Severity == SkinCompatibilitySeverity.Blocking);
        if (skinCompatibilityIssue != null)
        {
            IsPublishCompatible = false;
            PublishCompatibilityText = "不可发布：" + skinCompatibilityIssue.Message;
            return;
        }
        var skinCompatibilityIssue2 = source.FirstOrDefault(x => x.Severity == SkinCompatibilitySeverity.Warning);
        if (skinCompatibilityIssue2 != null)
        {
            IsPublishCompatible = true;
            PublishCompatibilityText = "可发布（有警告）：" + skinCompatibilityIssue2.Message;
        }
        else
        {
            IsPublishCompatible = true;
            PublishCompatibilityText = "可发布（兼容）";
        }
    }

    private void MirrorImage()
    {
        if (SelectedAction == null || SelectedItem == null)
        {
            return;
        }
        var text = Path.Combine(Manager.ProjectRootPath, "Resource", "CharacterSkin", SelectedItem.Name, "ClothCustom",
            SelectedAction.ActionName + ".png");
        if (!File.Exists(text))
        {
            MessageBox.Show("找不到对应的图片文件，无法翻转。", "错误");
            return;
        }
        try
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(text);
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.CreateOptions =
                BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
            var source = new TransformedBitmap(bitmapImage, new ScaleTransform(-1.0, 1.0));
            var pngBitmapEncoder = new PngBitmapEncoder();
            pngBitmapEncoder.Frames.Add(BitmapFrame.Create(source));
            using (var stream = new FileStream(text, FileMode.Create))
            {
                pngBitmapEncoder.Save(stream);
            }
            var selectedAction = SelectedAction;
            SelectedAction = null;
            SelectedAction = selectedAction;
        }
        catch (Exception ex)
        {
            MessageBox.Show("翻转失败: " + ex.Message + "\n可能文件正被占用。", "错误");
        }
    }

    private void Create()
    {
        if (string.IsNullOrWhiteSpace(Manager.ProjectRootPath))
        {
            MessageBox.Show("请先打开项目。", "提示");
            return;
        }
        var text = Interaction.InputBox("请输入新皮肤的名称 (英文):", "新建皮肤", "NewSkin");
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }
        try
        {
            var unifiedSkin = Manager.SkinRepo.Create(Manager.ProjectRootPath, text, Manager.CurrentModId);
            if (unifiedSkin != null)
            {
                SelectedItem = unifiedSkin;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("创建失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
        }
    }

    private void AddPresetAction()
    {
        if (SelectedItem == null)
        {
            return;
        }
        if (SelectedItem.IsVanilla)
        {
            MessageBox.Show("原版皮肤不可编辑。", "提示");
            return;
        }
        var text = SelectedPresetAction?.ToString();
        if (string.IsNullOrWhiteSpace(text))
        {
            MessageBox.Show("请先选择一个枚举动作。", "提示");
            return;
        }
        if (!SelectedItem.AddAction(text, out var createdAction, out var errorMessage))
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                MessageBox.Show(errorMessage, "提示");
            }
            return;
        }
        if (!string.IsNullOrWhiteSpace(Manager.ProjectRootPath))
        {
            Manager.SkinRepo.CancelActionImageDeletion(Manager.ProjectRootPath, SelectedItem.Name, text);
        }
        SelectedAction = createdAction;
        RefreshPublishCompatibility();
    }

    private void AddCustomAction()
    {
        if (SelectedItem == null)
        {
            return;
        }
        if (SelectedItem.IsVanilla)
        {
            MessageBox.Show("原版皮肤不可编辑。", "提示");
            return;
        }
        var text = Interaction.InputBox("请输入自定义动作名称 (例如: SuperSlash):", "添加动作", "NewAction");
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }
        text = text.Trim();
        try
        {
            XmlConvert.VerifyName(text);
        }
        catch (Exception ex)
        {
            MessageBox.Show("动作名不合法: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            return;
        }
        if (!SelectedItem.AddAction(text, out var createdAction, out var errorMessage))
        {
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                MessageBox.Show(errorMessage, "提示");
            }
            return;
        }
        if (!string.IsNullOrWhiteSpace(Manager.ProjectRootPath))
        {
            Manager.SkinRepo.CancelActionImageDeletion(Manager.ProjectRootPath, SelectedItem.Name, text);
        }
        SelectedAction = createdAction;
        RefreshPublishCompatibility();
    }

    private void DeleteAction()
    {
        if (SelectedItem == null || SelectedAction == null)
        {
            return;
        }
        if (SelectedItem.IsVanilla)
        {
            MessageBox.Show("原版皮肤不可编辑。", "提示");
        }
        else
        {
            if (MessageBox.Show("确认删除动作 [" + SelectedAction.ActionName + "] ?", "确认", MessageBoxButton.YesNo,
                    MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
            {
                return;
            }
            var selectedAction = SelectedAction;
            var actionName = selectedAction.ActionName;
            var value = SelectedItem.Actions.IndexOf(selectedAction);
            var flag = false;
            if (!string.IsNullOrWhiteSpace(Manager.ProjectRootPath))
            {
                if (File.Exists(Path.Combine(Manager.ProjectRootPath, "Resource", "CharacterSkin", SelectedItem.Name,
                        "ClothCustom", actionName + ".png")))
                {
                    var messageBoxResult =
                        MessageBox.Show("检测到该动作的图片文件。\n是否在保存时一并删除图片？\n\n是: 保存时删除图片\n否: 保留图片\n取消: 放弃本次删除", "动作图片处理",
                            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (messageBoxResult == MessageBoxResult.Cancel)
                    {
                        return;
                    }
                    flag = messageBoxResult == MessageBoxResult.Yes;
                }
            }
            if (!SelectedItem.RemoveAction(selectedAction))
            {
                MessageBox.Show("删除动作失败。", "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }
            if (!string.IsNullOrWhiteSpace(Manager.ProjectRootPath))
            {
                if (flag)
                {
                    Manager.SkinRepo.QueueActionImageDeletion(Manager.ProjectRootPath, SelectedItem.Name, actionName);
                }
                else
                {
                    Manager.SkinRepo.CancelActionImageDeletion(Manager.ProjectRootPath, SelectedItem.Name, actionName);
                }
            }
            if (SelectedItem.Actions.Count == 0)
            {
                SelectedAction = null;
                RefreshPublishCompatibility();
            }
            else
            {
                var index = Math.Clamp(value, 0, SelectedItem.Actions.Count - 1);
                SelectedAction = SelectedItem.Actions[index];
                RefreshPublishCompatibility();
            }
        }
    }

    private void DeleteSkin()
    {
        if (SelectedItem != null)
        {
            if (SelectedItem.IsVanilla)
            {
                MessageBox.Show("原版皮肤不可编辑。", "提示");
            }
            else if (MessageBox.Show("确认删除皮肤 [" + SelectedItem.Name + "] ?\n\n该操作会立即从当前列表移除。\n只有点击“保存”后才会真正删除磁盘文件。",
                         "确认删除皮肤", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
            {
                Manager.SkinRepo.Delete(SelectedItem);
                SelectedItem = null;
                SelectedAction = null;
                RefreshPublishCompatibility();
            }
        }
    }

    private void SetQuality(string qualityStr)
    {
        if (SelectedAction != null && int.TryParse(qualityStr, out var result))
        {
            SelectedAction.Quality = result;
        }
    }

    private void ImportImage()
    {
        if (SelectedItem == null || SelectedAction == null)
        {
            return;
        }
        var openFileDialog = new OpenFileDialog
        {
            Filter = "图片文件|*.png;*.jpg",
            Title = "选择图片用于动作: " + SelectedAction.ActionName
        };
        if (openFileDialog.ShowDialog() != true)
        {
            return;
        }
        try
        {
            var fileName = openFileDialog.FileName;
            var text = Path.Combine(Manager.ProjectRootPath, "Resource", "CharacterSkin", SelectedItem.Name,
                "ClothCustom");
            if (!Directory.Exists(text))
            {
                Directory.CreateDirectory(text);
            }
            var destFileName = Path.Combine(text, SelectedAction.ActionName + ".png");
            File.Copy(fileName, destFileName, true);
            try
            {
                using var bitmapStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                var bitmapFrame = BitmapDecoder
                    .Create(bitmapStream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.Default).Frames[0];
                var pixelWidth = bitmapFrame.PixelWidth;
                var pixelHeight = bitmapFrame.PixelHeight;
                SelectedAction.SizeX = pixelWidth;
                SelectedAction.SizeY = pixelHeight;
                if ((pixelWidth > 600 || pixelHeight > 600) && SelectedAction.Quality == 50)
                {
                    SelectedAction.Quality = 100;
                }
            }
            catch
            {
            }
            var selectedAction = SelectedAction;
            SelectedAction = null;
            SelectedAction = selectedAction;
            RefreshPublishCompatibility();
            MessageBox.Show("导入成功！\n已自动更新 XML 尺寸信息。", "提示");
        }
        catch (Exception ex)
        {
            MessageBox.Show("导入失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
        }
    }
}
