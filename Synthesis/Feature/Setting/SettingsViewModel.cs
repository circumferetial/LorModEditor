using System.IO;
using System.Windows;
using Microsoft.Win32;
using Synthesis.Core;
using Synthesis.Core.Log;

namespace Synthesis.Feature.Setting;

public class SettingsViewModel : BindableBase
{
    private readonly ProjectManager _manager;
    private bool _isReloading;

    public SettingsViewModel(ProjectManager manager)
    {
        _manager = manager;
        Config = EditorConfig.Instance;
        SaveCommand = new DelegateCommand(Save);
        ReloadCommand = new DelegateCommand(Reload, CanReload);
        BrowseCommand = new DelegateCommand(Browse);
    }

    public EditorConfig Config { get; }

    public DelegateCommand SaveCommand { get; }

    public DelegateCommand ReloadCommand { get; }

    public DelegateCommand BrowseCommand { get; }

    private bool CanReload() => !_isReloading;

    private void Save()
    {
        Config.Save();
        MessageBox.Show("设置已保存！\n部分设置（如语言、原版路径）需要重新加载项目才能生效。", "保存成功");
    }

    private async void Reload()
    {
        if (_isReloading)
        {
            return;
        }

        _isReloading = true;
        ReloadCommand.RaiseCanExecuteChanged();

        try
        {
            if (!string.IsNullOrEmpty(Config.LastProjectPath) && File.Exists(Config.LastProjectPath))
            {
                Config.Save();
                await _manager.OpenProject(Config.LastProjectPath);
            }
            else
            {
                MessageBox.Show("未找到上次打开的项目路径，请使用“打开项目”按钮手动加载。", "提示");
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Error: ", ex);
        }
        finally
        {
            _isReloading = false;
            ReloadCommand.RaiseCanExecuteChanged();
        }
    }

    private void Browse()
    {
        var openFolderDialog = new OpenFolderDialog
        {
            Title = "选择 BaseMod 文件夹 (LibraryOfRuina_Data/Managed/BaseMod)",
            Multiselect = false
        };
        if (!string.IsNullOrEmpty(Config.BaseModPath))
        {
            openFolderDialog.InitialDirectory = Config.BaseModPath;
        }
        if (openFolderDialog.ShowDialog() == true)
        {
            Config.BaseModPath = openFolderDialog.FolderName;
        }
    }
}
