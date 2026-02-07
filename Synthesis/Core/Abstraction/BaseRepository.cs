using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml.Linq;
using Synthesis.Core.Extensions;
using Synthesis.Core.Tools;

namespace Synthesis.Core.Abstraction;

public abstract class BaseRepository<T> : INotifyPropertyChanged where T : XWrapper
{
    // 默认容器
    protected readonly List<XDocument> _dataDocs = new();
    protected readonly List<XDocument> _locDocs = new();

    public ObservableCollection<T> Items { get; } = new();

    // 状态检查
    public virtual bool HasData => _dataDocs.Any(d => !d.IsVanilla());
    public virtual bool HasLoc => _locDocs.Any(d => !d.IsVanilla());

    // --- INotifyPropertyChanged ---
    public event PropertyChangedEventHandler? PropertyChanged;

    // --- 抽象接口：子类必须实现 ---

    /// <summary>
    ///     定义如何加载资源 (扫描哪些文件夹)
    /// </summary>
    public abstract void LoadResources(string projectRoot, string language, string modId);

    /// <summary>
    ///     定义如何创建默认文件
    /// </summary>
    public abstract void EnsureDefaults(string projectRoot, string language, string modId);

    /// <summary>
    ///     解析 XML 到 Items 列表
    /// </summary>
    public abstract void Load();

    // --- 通用方法 ---

    public virtual void Clear()
    {
        Items.Clear();
        _dataDocs.Clear();
        _locDocs.Clear();
        NotifyStatusChanged();
    }

    public virtual void AddDataDoc(XDocument doc)
    {
        _dataDocs.Add(doc);
        NotifyStatusChanged();
    }

    public virtual void AddLocDoc(XDocument doc)
    {
        _locDocs.Add(doc);
        NotifyStatusChanged();
    }

    public virtual void Delete(T item)
    {
        // 简单移除，具体 XML 删除逻辑建议在 Wrapper 内部或子类实现
        Items.Remove(item);
    }

    // 保存文件 (只保存当前 Mod 的文件)
    public virtual void SaveFiles(string currentModId)
    {
        SaveDocs(_dataDocs, currentModId);
        SaveDocs(_locDocs, currentModId);
    }

    protected void SaveDocs(List<XDocument> docs, string modId)
    {
        foreach (var doc in docs)
        {
            if (doc.GetPackageId() == modId)
            {
                // 获取加载时记录的路径
                var path = doc.Root?.Annotation<FilePathAnnotation>()?.Path;
                if (!string.IsNullOrEmpty(path))
                {
                    doc.Save(path);
                }
            }
        }
    }

    // --- 辅助工具：扫描并加载 ---
    protected void ScanAndLoad(string fullPath, string expectedRootName, string modId, Action<XDocument> addAction)
    {
        if (Directory.Exists(fullPath))
        {
            var patterns = new[] { "*.xml", "*.txt" };
            var files = patterns.SelectMany(ext =>
                Directory.EnumerateFiles(fullPath, ext, SearchOption.AllDirectories));

            foreach (var file in files)
            {
                try
                {
                    var doc = XDocument.Load(file);
                    // 校验根节点
                    if (doc.Root?.Name.LocalName == expectedRootName)
                    {
                        // 打标签
                        doc.Root.AddAnnotation("PID:" + modId);
                        doc.Root.AddAnnotation(new FilePathAnnotation(file));// 记录路径
                        addAction(doc);
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }
        else
        {
            MessageBox.Show($"not exist {fullPath} {Directory.Exists(fullPath)}");
        }
    }

    // --- 辅助工具：创建模板 ---
    protected void CreateXmlTemplate(string fullPath, string rootNodeName, string modId,
        Action<XDocument> registerAction)
    {
        if (File.Exists(fullPath)) return;
        
        try
        {
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement(rootNodeName));
            doc.Root?.AddAnnotation("PID:" + modId);
            doc.Root?.AddAnnotation(new FilePathAnnotation(fullPath));

            doc.Save(fullPath);
            registerAction(doc);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"创建文件失败: {fullPath}\n{ex.Message}");
        }
    }

    // 获取目标文件 (非原版)
    protected XDocument? GetTargetDataDoc(string rootName) =>
        _dataDocs.FirstOrDefault(d => d.Root?.Name.LocalName == rootName && !d.IsVanilla());

    protected XDocument? GetTargetLocDoc(string rootName) =>
        _locDocs.FirstOrDefault(d => d.Root?.Name.LocalName == rootName && !d.IsVanilla());

    protected void NotifyStatusChanged()
    {
        OnPropertyChanged(nameof(HasData));
        OnPropertyChanged(nameof(HasLoc));
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// 路径注解类