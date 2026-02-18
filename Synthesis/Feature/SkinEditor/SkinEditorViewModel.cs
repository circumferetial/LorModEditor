using System.IO;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Collections.Specialized;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Synthesis.Core;
using Synthesis.Core.Enums;

// 用于读取图片尺寸
// 引用 Wrappers
// 用于 Interaction.InputBox

// 用于 OpenFileDialog

namespace Synthesis.Feature.SkinEditor;

public class SkinEditorViewModel : BindableBase
{
    private UnifiedSkin? _subscribedSkin;

    public SkinEditorViewModel(ProjectManager manager)
    {
        Manager = manager;

        // 初始化命令
        CreateCommand = new DelegateCommand(Create);
        AddPresetActionCommand = new DelegateCommand(AddPresetAction, CanAddPresetAction)
            .ObservesProperty(() => SelectedItem)
            .ObservesProperty(() => SelectedPresetAction);
        DeleteActionCommand = new DelegateCommand(DeleteAction, CanDeleteAction)
            .ObservesProperty(() => SelectedItem)
            .ObservesProperty(() => SelectedAction);
        DeleteSkinCommand = new DelegateCommand(DeleteSkin, CanDeleteSkin)
            .ObservesProperty(() => SelectedItem);

        // 只有选中了皮肤和动作，才能导入图片
        ImportImageCommand = new DelegateCommand(ImportImage, () => SelectedAction != null && SelectedItem != null)
            .ObservesProperty(() => SelectedAction)
            .ObservesProperty(() => SelectedItem);
        SetQualityCommand = new DelegateCommand<string>(SetQuality);
        // 只有选中了非原版皮肤，才能添加动作
        AddCustomActionCommand = new DelegateCommand(AddCustomAction, CanEditSelectedSkin)
            .ObservesProperty(() => SelectedItem);

        // 音效命令
        MirrorImageCommand = new DelegateCommand(MirrorImage, () => SelectedAction != null && SelectedItem != null)
            .ObservesProperty(() => SelectedAction)
            .ObservesProperty(() => SelectedItem);

        RefreshPublishCompatibility();
    }

    // 【新增】属性
    public DelegateCommand MirrorImageCommand { get; }
    // =========================================================
    // 属性 (Properties)
    // =========================================================

    public ProjectManager Manager { get; }

    public IReadOnlyList<ActionDetail> PresetActions { get; } = SkinCompatibilityGuard.PresetActions;

    public UnifiedSkin? SelectedItem
    {
        get;
        set
        {
            var oldSkin = field;
            if (SetProperty(ref field, value))
            {
                if (oldSkin != null) DetachSkinSubscriptions(oldSkin);
                if (value != null) AttachSkinSubscriptions(value);
                SelectedAction = null;// 切换皮肤时，清空选中的动作
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


    // =========================================================
    // 命令 (Commands)
    // =========================================================

    public DelegateCommand CreateCommand { get; }
    public DelegateCommand AddPresetActionCommand { get; }
    public DelegateCommand AddCustomActionCommand { get; }
    public DelegateCommand DeleteActionCommand { get; }
    public DelegateCommand DeleteSkinCommand { get; }
    public DelegateCommand ImportImageCommand { get; }
    public DelegateCommand<string> SetQualityCommand { get; }

    private bool CanAddPresetAction() =>
        SelectedItem != null && !SelectedItem.IsVanilla && SelectedPresetAction != null;

    private bool CanEditSelectedSkin() => SelectedItem != null && !SelectedItem.IsVanilla;
    private bool CanDeleteAction() => SelectedItem != null && !SelectedItem.IsVanilla && SelectedAction != null;
    private bool CanDeleteSkin() => SelectedItem != null && !SelectedItem.IsVanilla;

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
        if (ReferenceEquals(_subscribedSkin, skin))
            _subscribedSkin = null;
    }

    private void OnSubscribedSkinPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(UnifiedSkin.Name))
            RefreshPublishCompatibility();
    }

    private void OnSubscribedSkinActionsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        RefreshPublishCompatibility();

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

        var issues = SkinCompatibilityGuard.Validate(SelectedItem.Element);
        var blockingIssue = issues.FirstOrDefault(x => x.Severity == SkinCompatibilitySeverity.Blocking);
        if (blockingIssue != null)
        {
            IsPublishCompatible = false;
            PublishCompatibilityText = $"不可发布：{blockingIssue.Message}";
            return;
        }

        var warningIssue = issues.FirstOrDefault(x => x.Severity == SkinCompatibilitySeverity.Warning);
        if (warningIssue != null)
        {
            IsPublishCompatible = true;
            PublishCompatibilityText = $"可发布（有警告）：{warningIssue.Message}";
            return;
        }

        IsPublishCompatible = true;
        PublishCompatibilityText = "可发布（兼容）";
    }

    // 【核心逻辑】物理翻转图片文件
    private void MirrorImage()
    {
        if (SelectedAction == null || SelectedItem == null) return;

        // 1. 获取图片路径
        var path = Path.Combine(Manager.ProjectRootPath!, "Resource", "CharacterSkin", SelectedItem.Name,
            "ClothCustom", $"{SelectedAction.ActionName}.png");

        if (!File.Exists(path))
        {
            MessageBox.Show("找不到对应的图片文件，无法翻转。", "错误");
            return;
        }

        try
        {
            // 2. 读取原图 (使用 OnLoad 模式以避免文件被锁定，虽然我们稍后要覆盖它)
            // 这里我们需要创建一个新的 BitmapImage 并立即冻结它，以便在非 UI 线程或后续操作中使用
            var src = new BitmapImage();
            src.BeginInit();
            src.UriSource = new Uri(path);
            src.CacheOption = BitmapCacheOption.OnLoad;// 关键：加载完立即释放文件句柄
            src.CreateOptions = BitmapCreateOptions.PreservePixelFormat | BitmapCreateOptions.IgnoreColorProfile;
            src.EndInit();
            src.Freeze();

            // 3. 执行水平翻转 (ScaleX = -1)
            var flippedBitmap = new TransformedBitmap(src, new ScaleTransform(-1, 1));

            // 4. 保存回文件 (覆盖原文件)
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(flippedBitmap));

            using (var stream = new FileStream(path, FileMode.Create))
            {
                encoder.Save(stream);
            }

            // 5. 触发 UI 刷新
            // 小技巧：通过重新赋值 SelectedAction 来触发 View 的 SelectionChanged，从而重新加载图片
            var temp = SelectedAction;
            SelectedAction = null;
            SelectedAction = temp;

            // 提示用户 (可选)
            // MessageBox.Show("图片已镜像翻转！");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"翻转失败: {ex.Message}\n可能文件正被占用。", "错误");
        }
    }


    private void Create()
    {
        if (string.IsNullOrWhiteSpace(Manager.ProjectRootPath))
        {
            MessageBox.Show("请先打开项目。", "提示");
            return;
        }

        var name = Interaction.InputBox("请输入新皮肤的名称 (英文):", "新建皮肤", "NewSkin");
        if (string.IsNullOrWhiteSpace(name)) return;

        try
        {
            var newItem = Manager.SkinRepo.Create(Manager.ProjectRootPath!, name, Manager.CurrentModId);
            if (newItem != null)
            {
                SelectedItem = newItem;// 自动选中新建项
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"创建失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AddPresetAction()
    {
        if (SelectedItem == null) return;
        if (SelectedItem.IsVanilla)
        {
            MessageBox.Show("原版皮肤不可编辑。", "提示");
            return;
        }

        var actionName = SelectedPresetAction?.ToString();
        if (string.IsNullOrWhiteSpace(actionName))
        {
            MessageBox.Show("请先选择一个枚举动作。", "提示");
            return;
        }

        if (!SelectedItem.AddAction(actionName, out var createdAction, out var error))
        {
            if (!string.IsNullOrWhiteSpace(error))
                MessageBox.Show(error, "提示");
            return;
        }

        if (!string.IsNullOrWhiteSpace(Manager.ProjectRootPath))
            Manager.SkinRepo.CancelActionImageDeletion(Manager.ProjectRootPath, SelectedItem.Name, actionName);

        SelectedAction = createdAction;
        RefreshPublishCompatibility();
    }

    private void AddCustomAction()
    {
        if (SelectedItem == null) return;
        if (SelectedItem.IsVanilla)
        {
            MessageBox.Show("原版皮肤不可编辑。", "提示");
            return;
        }

        var actionName = Interaction.InputBox("请输入自定义动作名称 (例如: SuperSlash):", "添加动作", "NewAction");
        if (string.IsNullOrWhiteSpace(actionName)) return;
        actionName = actionName.Trim();

        try
        {
            XmlConvert.VerifyName(actionName);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"动作名不合法: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!SelectedItem.AddAction(actionName, out var createdAction, out var error))
        {
            if (!string.IsNullOrWhiteSpace(error))
                MessageBox.Show(error, "提示");
            return;
        }

        if (!string.IsNullOrWhiteSpace(Manager.ProjectRootPath))
            Manager.SkinRepo.CancelActionImageDeletion(Manager.ProjectRootPath, SelectedItem.Name, actionName);

        SelectedAction = createdAction;
        RefreshPublishCompatibility();
    }

    private void DeleteAction()
    {
        if (SelectedItem == null || SelectedAction == null) return;
        if (SelectedItem.IsVanilla)
        {
            MessageBox.Show("原版皮肤不可编辑。", "提示");
            return;
        }

        if (MessageBox.Show($"确认删除动作 [{SelectedAction.ActionName}] ?", "确认",
                MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        var actionToDelete = SelectedAction;
        var actionName = actionToDelete.ActionName;
        var selectedIndex = SelectedItem.Actions.IndexOf(actionToDelete);

        var shouldDeleteImageOnSave = false;
        if (!string.IsNullOrWhiteSpace(Manager.ProjectRootPath))
        {
            var imagePath = Path.Combine(Manager.ProjectRootPath, "Resource", "CharacterSkin", SelectedItem.Name,
                "ClothCustom", $"{actionName}.png");
            if (File.Exists(imagePath))
            {
                var imageChoice = MessageBox.Show(
                    "检测到该动作的图片文件。\n是否在保存时一并删除图片？\n\n是: 保存时删除图片\n否: 保留图片\n取消: 放弃本次删除",
                    "动作图片处理", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (imageChoice == MessageBoxResult.Cancel) return;
                shouldDeleteImageOnSave = imageChoice == MessageBoxResult.Yes;
            }
        }

        if (!SelectedItem.RemoveAction(actionToDelete))
        {
            MessageBox.Show("删除动作失败。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!string.IsNullOrWhiteSpace(Manager.ProjectRootPath))
        {
            if (shouldDeleteImageOnSave)
                Manager.SkinRepo.QueueActionImageDeletion(Manager.ProjectRootPath, SelectedItem.Name, actionName);
            else
                Manager.SkinRepo.CancelActionImageDeletion(Manager.ProjectRootPath, SelectedItem.Name, actionName);
        }

        if (SelectedItem.Actions.Count == 0)
        {
            SelectedAction = null;
            RefreshPublishCompatibility();
            return;
        }

        var nextIndex = Math.Clamp(selectedIndex, 0, SelectedItem.Actions.Count - 1);
        SelectedAction = SelectedItem.Actions[nextIndex];
        RefreshPublishCompatibility();
    }

    private void DeleteSkin()
    {
        if (SelectedItem == null) return;
        if (SelectedItem.IsVanilla)
        {
            MessageBox.Show("原版皮肤不可编辑。", "提示");
            return;
        }

        if (MessageBox.Show(
                $"确认删除皮肤 [{SelectedItem.Name}] ?\n\n该操作会立即从当前列表移除。\n只有点击“保存”后才会真正删除磁盘文件。",
                "确认删除皮肤", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
            return;

        Manager.SkinRepo.Delete(SelectedItem);
        SelectedItem = null;
        SelectedAction = null;
        RefreshPublishCompatibility();
    }

    // 【新增】命令逻辑
    private void SetQuality(string qualityStr)
    {
        if (SelectedAction != null && int.TryParse(qualityStr, out var val))
        {
            SelectedAction.Quality = val;
        }
    }

    private void ImportImage()
    {
        if (SelectedItem == null || SelectedAction == null) return;

        var dialog = new OpenFileDialog
        {
            Filter = "图片文件|*.png;*.jpg",
            Title = $"选择图片用于动作: {SelectedAction.ActionName}"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var sourceFile = dialog.FileName;

                // 目标路径: Resource/CharacterSkin/{SkinName}/ClothCustom/{Action}.png
                var targetDir = Path.Combine(Manager.ProjectRootPath!, "Resource", "CharacterSkin", SelectedItem.Name,
                    "ClothCustom");
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                var targetFile = Path.Combine(targetDir, $"{SelectedAction.ActionName}.png");

                // 复制并覆盖
                File.Copy(sourceFile, targetFile, true);

                // --- 逻辑升级：读取图片尺寸并写入 XML (适配 ExtendedLoader) ---
                // 虽然 View 层加载图片时也会做，但在这里做一次更稳健
                try
                {
                    using var stream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreColorProfile,
                        BitmapCacheOption.Default);
                    var frame = decoder.Frames[0];
                    var w = frame.PixelWidth;
                    var h = frame.PixelHeight;

                    // 1. 自动写入尺寸到 XML
                    SelectedAction.SizeX = w;
                    SelectedAction.SizeY = h;

                    // 2. 智能设置 Quality (可选优化)
                    // 如果图片大于 512，通常意味着这是高清图，建议把 Quality 设为 100 (1:1比例)
                    // 否则保持默认 50 (2:1比例)
                    if (w > 600 || h > 600)
                    {
                        // 只有当原本是默认值50的时候才自动改，防止覆盖用户手动设定的奇葩值
                        if (SelectedAction.Quality == 50)
                        {
                            SelectedAction.Quality = 100;
                        }
                    }
                }
                catch
                {
                    // 忽略读取错误，不影响文件复制
                }

                // 触发 UI 刷新 (Hack: 重新赋值 SelectedAction 触发 View 的 SelectionChanged)
                var temp = SelectedAction;
                SelectedAction = null;
                SelectedAction = temp;
                RefreshPublishCompatibility();

                MessageBox.Show("导入成功！\n已自动更新 XML 尺寸信息。", "提示");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
