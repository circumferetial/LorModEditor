using System.IO;
using System.Windows;
using LorModEditor.Core;
using LorModEditor.Core.Log;
using Microsoft.Win32;

// 【关键】使用 WPF 原生对话框命名空间

namespace LorModEditor.ViewModels;

public class SettingsViewModel : BindableBase
{
    private readonly ProjectManager _manager;

    public SettingsViewModel(ProjectManager manager)
    {
        _manager = manager;

        // 直接引用单例
        Config = EditorConfig.Instance;

        SaveCommand = new DelegateCommand(Save);
        ReloadCommand = new DelegateCommand(Reload);
        BrowseCommand = new DelegateCommand(Browse);
    }

    // 绑定源
    public EditorConfig Config { get; }

    public DelegateCommand SaveCommand { get; }
    public DelegateCommand ReloadCommand { get; }
    public DelegateCommand BrowseCommand { get; }

    private void Save()
    {
        Config.Save();
        MessageBox.Show("设置已保存！\n部分设置（如语言、原版路径）需要重新加载项目才能生效。", "保存成功");
    }

    private async void Reload()
    {
        try
        {
            if (!string.IsNullOrEmpty(Config.LastProjectPath) && File.Exists(Config.LastProjectPath))
            {
                // 保存当前配置再重载
                Config.Save();
                await _manager.OpenProject(Config.LastProjectPath);
            }
            else
            {
                MessageBox.Show("未找到上次打开的项目路径，请使用“打开项目”按钮手动加载。", "提示");
            }
        }
        catch (Exception e)
        {
            Logger.Error("Error: ", e);
        }
    }

    private void Browse()
    {
        // 【关键修改】使用 WPF 原生文件夹选择器 (仅限 .NET Core 3.1 / .NET 5+ / .NET 8)
        var dialog = new OpenFolderDialog
        {
            Title = "选择 BaseMod 文件夹 (LibraryOfRuina_Data/Managed/BaseMod)",
            Multiselect = false
        };

        // 如果已经有路径，尝试设置初始目录
        if (!string.IsNullOrEmpty(Config.BaseModPath))
        {
            dialog.InitialDirectory = Config.BaseModPath;
        }

        if (dialog.ShowDialog() == true)
        {
            // 因为 Config 继承了 BindableBase，这里赋值后，界面会自动更新
            Config.BaseModPath = dialog.FolderName;
        }
    }
}