using System.IO;
using System.IO.Compression;
using System.Windows;
using Synthesis.Core;

// 需要引用 System.IO.Compression.FileSystem

namespace Synthesis.Plugins.Backup.ViewModels;

public class BackupButtonViewModel : BindableBase
{
    private readonly ProjectManager _manager;

    public BackupButtonViewModel(ProjectManager manager)
    {
        _manager = manager;
        BackupCommand = new DelegateCommand(Backup);
    }

    public DelegateCommand BackupCommand { get; }

    private void Backup()
    {
        var sourceDir = _manager.ProjectRootPath;
        if (string.IsNullOrEmpty(sourceDir) || !Directory.Exists(sourceDir))
        {
            MessageBox.Show("请先打开一个项目！");
            return;
        }

        try
        {
            // 创建备份目录
            var backupRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            if (!Directory.Exists(backupRoot)) Directory.CreateDirectory(backupRoot);

            // 生成文件名: ModID_时间.zip
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var modId = _manager.CurrentModId;
            var zipPath = Path.Combine(backupRoot, $"{modId}_{timestamp}.zip");

            // 压缩 (需要引用 System.IO.Compression.ZipFile)
            ZipFile.CreateFromDirectory(sourceDir, zipPath);

            MessageBox.Show($"备份成功！\n保存在: {zipPath}", "备份完成");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"备份失败: {ex.Message}");
        }
    }
}