using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;
using Synthesis.Core;
using Synthesis.Core.Log;
using DesignExporter = Synthesis.Core.Tools.DesignExporter;

// 用于 SaveFileDialog

// 引用 DesignExporter

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

        // 【新增】导出命令
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

    // 【新增】导出命令属性
    public DelegateCommand ExportCommand { get; }

    private void Navigate(string viewName)
    {
        if (string.IsNullOrEmpty(viewName)) return;
        _regionManager.RequestNavigate("ContentRegion", viewName);
    }

    private async void OpenProject()
    {
        try
        {
            var dialog = new OpenFileDialog
            {
                Filter = "StageModInfo|StageModInfo.xml",
                Title = "打开 Mod 项目"
            };

            if (dialog.ShowDialog() == true)
            {
                await _projectManager.OpenProject(dialog.FileName);
                Title = $"LoR Mod Editor - {_projectManager.CurrentModId}";
                Navigate("CardEditorView");
            }
        }
        catch (Exception e)
        {
            Logger.Error("Error: ", e);
        }
    }

    private void SaveProject()
    {
        _projectManager.SaveAll();
        MessageBox.Show("保存成功！");
    }

    // 【新增】导出逻辑
    private void ExportProject()
    {
        if (string.IsNullOrEmpty(_projectManager.ProjectRootPath))
        {
            MessageBox.Show("请先打开一个项目！");
            return;
        }

        // 1. 选择保存位置
        var dialog = new SaveFileDialog
        {
            Title = "导出设计文档",
            FileName = $"{_projectManager.CurrentModId}_Design.md",// 默认文件名
            Filter = "Markdown 文档|*.md|文本文件|*.txt",
            DefaultExt = ".md"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                // 2. 调用服务生成文件
                DesignExporter.ExportToMarkdown(_projectManager, dialog.FileName);

                // 3. 询问是否打开
                if (MessageBox.Show("导出成功！是否立即打开查看？", "完成", MessageBoxButton.YesNo, MessageBoxImage.Information) ==
                    MessageBoxResult.Yes)
                {
                    // 调用系统默认编辑器打开
                    new Process
                    {
                        StartInfo = new ProcessStartInfo(dialog.FileName) { UseShellExecute = true }
                    }.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"导出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}