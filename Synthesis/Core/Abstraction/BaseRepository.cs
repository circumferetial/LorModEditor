using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Xml.Linq;
using Synthesis.Core.Extensions;
using Synthesis.Core.Tools;

namespace Synthesis.Core.Abstraction;

public abstract class BaseRepository<T> : INotifyPropertyChanged, IGameRepository, IVanillaDataLoadable
    where T : XWrapper
{
    protected readonly List<XDocument> _modDataDocs = [];

    protected readonly List<XDocument> _modLocDocs = [];

    protected readonly List<XDocument> _vanillaDataDocs = [];

    protected readonly List<XDocument> _vanillaLocDocs = [];

    protected IEnumerable<XDocument> _dataDocs => _vanillaDataDocs.Concat(_modDataDocs);

    protected IEnumerable<XDocument> _locDocs => _vanillaLocDocs.Concat(_modLocDocs);

    public ObservableCollection<T> Items { get; } = [];

    public virtual bool HasModData => _modDataDocs.Count > 0;

    public virtual bool HasModLoc => _modLocDocs.Count > 0;

    public virtual void LoadResources(string projectRoot, string language, string modId)
    {
        LoadDataResources(projectRoot, modId);
        LoadLocResources(projectRoot, language, modId);
    }

    public virtual void LoadLocResources(string projectRoot, string language, string modId)
    {
    }

    public abstract void EnsureDefaults(string projectRoot, string language, string modId);

    public abstract void Parse(bool containOriginal);

    public virtual void ClearLocOnly()
    {
        _modLocDocs.Clear();
        _vanillaLocDocs.Clear();
    }

    public virtual void ClearModOnly()
    {
        Items.Clear();
        _modDataDocs.Clear();
        _modLocDocs.Clear();
        NotifyStatusChanged();
    }

    public virtual void ClearAll()
    {
        Items.Clear();
        _vanillaDataDocs.Clear();
        _vanillaLocDocs.Clear();
        _modDataDocs.Clear();
        _modLocDocs.Clear();
        NotifyStatusChanged();
    }

    public virtual void SaveFiles(string currentModId)
    {
        SaveDocs(_modDataDocs, currentModId);
        SaveDocs(_modLocDocs, currentModId);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public virtual void LoadDataResources(string projectRoot, string modId)
    {
    }

    public void AddDataDoc(XDocument doc)
    {
        if (doc.IsVanilla())
        {
            _vanillaDataDocs.Add(doc);
        }
        else
        {
            _modDataDocs.Add(doc);
        }
        NotifyStatusChanged();
    }

    public void AddLocDoc(XDocument doc)
    {
        if (doc.IsVanilla())
            _vanillaLocDocs.Add(doc);
        else
            _modLocDocs.Add(doc);
        NotifyStatusChanged();
    }

    public virtual void Delete(T item)
    {
        Items.Remove(item);
    }

    protected void SaveDocs(IEnumerable<XDocument> docs, string modId)
    {
        foreach (var doc in docs)
        {
            if (doc.GetPackageId() == modId)
            {
                var text = doc.Root?.Annotation<FilePathAnnotation>()?.Path;
                if (!string.IsNullOrEmpty(text))
                {
                    doc.Save(text);
                }
            }
        }
    }

    private bool IsDocumentRegistered(string fullPath)
    {
        var fullPath2 = Path.GetFullPath(fullPath);
        return _vanillaDataDocs.Concat(_vanillaLocDocs).Concat(_modDataDocs).Concat(_modLocDocs)
            .Select(item => item.Root?.Annotation<FilePathAnnotation>()?.Path).Any(text =>
                !string.IsNullOrWhiteSpace(text) && string.Equals(Path.GetFullPath(text), fullPath2,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static void AnnotateRuntimeInfo(XDocument doc, string modId, string fullPath)
    {
        if (doc.Root != null)
        {
            doc.Root.AddAnnotation("PID:" + modId);
            doc.Root.AddAnnotation(new FilePathAnnotation(fullPath));
        }
    }

    private static string GetFallbackTemplatePath(string fullPath)
    {
        var path = Path.GetDirectoryName(fullPath) ?? string.Empty;
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullPath);
        var extension = Path.GetExtension(fullPath);
        return Path.Combine(path, fileNameWithoutExtension + ".synthesis" + extension);
    }

    protected void ScanAndLoad(string fullPath, string expectedRootName, string modId, Action<XDocument> addAction)
    {
        if (!Directory.Exists(fullPath))
        {
            return;
        }
        foreach (var item in new string[2] { "*.xml", "*.txt" }.SelectMany(ext =>
                     Directory.EnumerateFiles(fullPath, ext, SearchOption.AllDirectories)))
        {
            try
            {
                var xDocument = XDocument.Load(item);
                if (!string.Equals(xDocument.Root?.Name.LocalName, expectedRootName,
                        StringComparison.OrdinalIgnoreCase)) continue;
                AnnotateRuntimeInfo(xDocument, modId, item);
                addAction(xDocument);
            }
            catch
            {
            }
        }
    }

    protected void CreateXmlTemplate(string fullPath, string rootNodeName, string modId,
        Action<XDocument> registerAction)
    {
        fullPath = Path.GetFullPath(fullPath);
        if (IsDocumentRegistered(fullPath))
        {
            return;
        }
        try
        {
            if (File.Exists(fullPath))
            {
                var xDocument = XDocument.Load(fullPath);
                if (string.Equals(xDocument.Root?.Name.LocalName, rootNodeName, StringComparison.OrdinalIgnoreCase))
                {
                    AnnotateRuntimeInfo(xDocument, modId, fullPath);
                    registerAction(xDocument);
                    return;
                }
                var fallbackTemplatePath = GetFallbackTemplatePath(fullPath);
                if (IsDocumentRegistered(fallbackTemplatePath))
                {
                    return;
                }
                if (File.Exists(fallbackTemplatePath))
                {
                    var xDocument2 = XDocument.Load(fallbackTemplatePath);
                    if (string.Equals(xDocument2.Root?.Name.LocalName, rootNodeName,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        AnnotateRuntimeInfo(xDocument2, modId, fallbackTemplatePath);
                        registerAction(xDocument2);
                    }
                }
                else
                {
                    var xDocument3 = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement(rootNodeName));
                    AnnotateRuntimeInfo(xDocument3, modId, fallbackTemplatePath);
                    xDocument3.Save(fallbackTemplatePath);
                    registerAction(xDocument3);
                }
            }
            else
            {
                var directoryName = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
                var xDocument4 = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement(rootNodeName));
                AnnotateRuntimeInfo(xDocument4, modId, fullPath);
                xDocument4.Save(fullPath);
                registerAction(xDocument4);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("创建文件失败: " + fullPath + "\n" + ex.Message);
        }
    }

    protected XDocument? GetTargetDataDoc(string rootName)
    {
        return _modDataDocs.FirstOrDefault(d =>
            string.Equals(d.Root?.Name.LocalName, rootName, StringComparison.OrdinalIgnoreCase));
    }

    protected XDocument? GetTargetLocDoc(string rootName)
    {
        return _modLocDocs.FirstOrDefault(d =>
            string.Equals(d.Root?.Name.LocalName, rootName, StringComparison.OrdinalIgnoreCase));
    }

    protected void NotifyStatusChanged()
    {
        OnPropertyChanged("HasModData");
        OnPropertyChanged("HasModLoc");
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
