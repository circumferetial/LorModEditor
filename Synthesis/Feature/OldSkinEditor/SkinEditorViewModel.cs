using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Synthesis.Core;

// 用于读取图片尺寸
// 引用 Wrappers
// 用于 Interaction.InputBox

// 用于 OpenFileDialog

namespace Synthesis.Feature.OldSkinEditor;

public class SkinEditorViewModel : BindableBase
{
    public SkinEditorViewModel(ProjectManager manager)
    {
        Manager = manager;

        // 初始化命令
        CreateCommand = new DelegateCommand(Create);

        // 只有选中了皮肤和动作，才能导入图片
        ImportImageCommand = new DelegateCommand(ImportImage, () => SelectedAction != null && SelectedItem != null)
            .ObservesProperty(() => SelectedAction)
            .ObservesProperty(() => SelectedItem);

        // 只有选中了皮肤，才能添加动作
        AddCustomActionCommand = new DelegateCommand(AddCustomAction, () => SelectedItem != null)
            .ObservesProperty(() => SelectedItem);

        // 音效命令
    }

    // =========================================================
    // 属性 (Properties)
    // =========================================================

    public ProjectManager Manager { get; }

    public UnifiedSkin? SelectedItem
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                SelectedAction = null;// 切换皮肤时，清空选中的动作
            }
        }
    }

    public UnifiedSkinAction? SelectedAction
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool IsHDMode
    {
        get;
        set => SetProperty(ref field, value);
        // View 的 Code-Behind 会监听这个属性变化来调整图片显示模式
    }

    // =========================================================
    // 命令 (Commands)
    // =========================================================

    public DelegateCommand CreateCommand { get; }
    public DelegateCommand AddCustomActionCommand { get; }
    public DelegateCommand ImportImageCommand { get; }


    private void Create()
    {
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

    private void AddCustomAction()
    {
        if (SelectedItem == null) return;

        var actionName = Interaction.InputBox("请输入自定义动作名称 (例如: SuperSlash):", "添加动作", "NewAction");
        if (string.IsNullOrWhiteSpace(actionName)) return;

        // 查重
        if (SelectedItem.Actions.Any(x => x.ActionName.Equals(actionName, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("该动作已存在！");
            return;
        }

        // 添加动作 (UnifiedSkin.AddAction 方法会自动初始化 Pivot/Head/Size 等节点)
        SelectedItem.AddAction(actionName);

        // 自动选中新动作
        SelectedAction = SelectedItem.Actions.FirstOrDefault(x => x.ActionName == actionName);
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
                    using (var stream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read))
                    {
                        var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.IgnoreColorProfile,
                            BitmapCacheOption.Default);
                        var frame = decoder.Frames[0];

                        SelectedAction.SizeX = frame.PixelWidth;
                        SelectedAction.SizeY = frame.PixelHeight;
                        // Quality 默认保持 50，除非用户手动去改
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

                MessageBox.Show("导入成功！\n已自动更新 XML 尺寸信息。", "提示");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导入失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}