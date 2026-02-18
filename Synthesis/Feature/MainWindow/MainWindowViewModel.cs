using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;
using Synthesis.Core;
using Synthesis.Core.Log;
using Synthesis.Core.Tools;

namespace Synthesis.Feature.MainWindow;

public class MainWindowViewModel : BindableBase
{
    private readonly ProjectManager _projectManager;

    private readonly IRegionManager _regionManager;

    public MainWindowViewModel(ProjectManager projectManager, IRegionManager regionManager)
    {
        _projectManager = projectManager;
        _regionManager = regionManager;
        NavigateCommand = new DelegateCommand<string>(Navigate);
        OpenProjectCommand = new DelegateCommand(OpenProject);
        SaveProjectCommand = new DelegateCommand(SaveProject);
        ExportCommand = new DelegateCommand(ExportProject);
    }

    public string Title
    {
        get;
        set => SetProperty(ref field, value);
    } = "LoR Mod Editor v1.0";

    public DelegateCommand<string> NavigateCommand { get; }

    public DelegateCommand OpenProjectCommand { get; }

    public DelegateCommand SaveProjectCommand { get; }

    public DelegateCommand ExportCommand { get; }

    private void Navigate(string viewName)
    {
        if (!string.IsNullOrEmpty(viewName))
        {
            _regionManager.RequestNavigate("ContentRegion", viewName);
        }
    }

    private async void OpenProject()
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "StageModInfo|StageModInfo.xml",
                Title = "打开 Mod 项目"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                await _projectManager.OpenProject(openFileDialog.FileName);
                Title = "LoR Mod Editor - " + _projectManager.CurrentModId;
                Navigate("CardEditorView");
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Error: ", ex);
        }
    }

    private void SaveProject()
    {
        _projectManager.SaveAll();
        MessageBox.Show("保存成功！");
    }

    private void ExportProject()
    {
        if (string.IsNullOrEmpty(_projectManager.ProjectRootPath))
        {
            MessageBox.Show("请先打开一个项目！");
            return;
        }
        var saveFileDialog = new SaveFileDialog
        {
            Title = "导出设计文档",
            FileName = _projectManager.CurrentModId + "_Design.md",
            Filter = "Markdown 文档|*.md|文本文件|*.txt",
            DefaultExt = ".md"
        };
        if (saveFileDialog.ShowDialog() != true)
        {
            return;
        }
        try
        {
            DesignExporter.ExportToMarkdown(_projectManager, saveFileDialog.FileName);
            if (MessageBox.Show("导出成功！是否立即打开查看？", "完成", MessageBoxButton.YesNo, MessageBoxImage.Asterisk) ==
                MessageBoxResult.Yes)
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo(saveFileDialog.FileName)
                {
                    UseShellExecute = true
                };
                process.Start();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("导出失败: " + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
        }
    }
}
