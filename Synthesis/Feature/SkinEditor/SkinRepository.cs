using System.IO;
using System.Windows;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;
using Synthesis.Core.Enums;
using Synthesis.Core.Extensions;
using Synthesis.Core.Log;
using Synthesis.Core.Tools;

namespace Synthesis.Feature.SkinEditor;

public class SkinRepository : BaseRepository<UnifiedSkin>
{
    private readonly HashSet<string> _pendingDeleteActionImages = new(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<string> _pendingDeleteSkinDirs = new(StringComparer.OrdinalIgnoreCase);

    private static string NormalizePath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return path;
        }
    }

    private static string EnsureTrailingSeparator(string path)
    {
        if (!path.EndsWith(Path.DirectorySeparatorChar) && !path.EndsWith(Path.AltDirectorySeparatorChar))
        {
            return path + Path.DirectorySeparatorChar;
        }
        return path;
    }

    private static string? GetDocPath(XDocument doc) => doc.Root?.Annotation<FilePathAnnotation>()?.Path;

    public override void LoadResources(string root, string lang, string modId)
    {
        LoadDataResources(root, modId);
    }

    public override void LoadDataResources(string projectRoot, string modId)
    {
        var path = Path.Combine(projectRoot, "Resource", "CharacterSkin");
        if (!Directory.Exists(path))
        {
            return;
        }
        var directories = Directory.GetDirectories(path);
        for (var i = 0; i < directories.Length; i++)
        {
            var text = Path.Combine(directories[i], "ModInfo.xml");
            if (!File.Exists(text))
            {
                continue;
            }
            try
            {
                var xDocument = XDocument.Load(text);
                if (!(xDocument.Root?.Name.LocalName != "ModInfo"))
                {
                    xDocument.Root.AddAnnotation("PID:" + modId);
                    xDocument.Root.AddAnnotation(new FilePathAnnotation(text));
                    AddDataDoc(xDocument);
                }
            }
            catch (Exception ex)
            {
                Logger.Error("SkinLoad", ex);
            }
        }
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
    }

    public override void Parse(bool containOriginal)
    {
        Items.Clear();
        IEnumerable<XDocument> enumerable;
        if (!containOriginal)
        {
            IEnumerable<XDocument> modDataDocs = _modDataDocs;
            enumerable = modDataDocs;
        }
        else
        {
            enumerable = _dataDocs;
        }
        foreach (var item in enumerable)
        {
            if (!(item.Root?.Name.LocalName != "ModInfo"))
            {
                Items.Add(new UnifiedSkin(item.Root));
            }
        }
    }

    public UnifiedSkin? Create(string projectRoot, string skinName, string modId)
    {
        var text = Path.Combine(projectRoot, "Resource", "CharacterSkin", skinName);
        if (!Directory.Exists(text))
        {
            Directory.CreateDirectory(text);
        }
        var text2 = Path.Combine(text, "ModInfo.xml");
        if (File.Exists(text2))
        {
            return null;
        }
        var xDocument = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("ModInfo"));
        var xElement = new XElement("ClothInfo");
        xElement.Add(new XElement("Name", skinName));
        xElement.Add(SkinCompatibilityGuard.CreateStandardActionNode(ActionDetail.Default.ToString()));
        xDocument.Root.Add(xElement);
        xDocument.Save(text2);
        xDocument.Root.AddAnnotation("PID:" + modId);
        xDocument.Root.AddAnnotation(new FilePathAnnotation(text2));
        AddDataDoc(xDocument);
        var unifiedSkin = new UnifiedSkin(xDocument.Root);
        Items.Add(unifiedSkin);
        return unifiedSkin;
    }

    public override void Delete(UnifiedSkin item)
    {
        if (item.IsVanilla)
        {
            return;
        }
        if (item.Element.Document != null)
        {
            _modDataDocs.Remove(item.Element.Document);
        }
        var text = item.Element.Document?.Root?.Annotation<FilePathAnnotation>()?.Path;
        var normalizedDocPath = string.IsNullOrWhiteSpace(text) ? null : NormalizePath(text);
        if (normalizedDocPath != null)
        {
            _modDataDocs.RemoveAll(delegate(XDocument doc)
            {
                var docPath = GetDocPath(doc);
                return !string.IsNullOrWhiteSpace(docPath) && string.Equals(NormalizePath(docPath), normalizedDocPath,
                    StringComparison.OrdinalIgnoreCase);
            });
            var directoryName = Path.GetDirectoryName(normalizedDocPath);
            if (!string.IsNullOrWhiteSpace(directoryName))
            {
                var text2 = NormalizePath(directoryName);
                _pendingDeleteSkinDirs.Add(text2);
                var folderPrefix = EnsureTrailingSeparator(text2);
                _pendingDeleteActionImages.RemoveWhere(path =>
                    NormalizePath(path).StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase));
            }
        }
        base.Delete(item);
        NotifyStatusChanged();
    }

    public void QueueActionImageDeletion(string projectRoot, string skinName, string actionName)
    {
        if (!string.IsNullOrWhiteSpace(projectRoot) && !string.IsNullOrWhiteSpace(skinName) &&
            !string.IsNullOrWhiteSpace(actionName))
        {
            var path = Path.Combine(projectRoot, "Resource", "CharacterSkin", skinName, "ClothCustom",
                actionName + ".png");
            _pendingDeleteActionImages.Add(NormalizePath(path));
        }
    }

    public void CancelActionImageDeletion(string projectRoot, string skinName, string actionName)
    {
        if (!string.IsNullOrWhiteSpace(projectRoot) && !string.IsNullOrWhiteSpace(skinName) &&
            !string.IsNullOrWhiteSpace(actionName))
        {
            var path = Path.Combine(projectRoot, "Resource", "CharacterSkin", skinName, "ClothCustom",
                actionName + ".png");
            _pendingDeleteActionImages.Remove(NormalizePath(path));
        }
    }

    public override void ClearModOnly()
    {
        base.ClearModOnly();
        _pendingDeleteSkinDirs.Clear();
        _pendingDeleteActionImages.Clear();
    }

    public override void ClearAll()
    {
        base.ClearAll();
        _pendingDeleteSkinDirs.Clear();
        _pendingDeleteActionImages.Clear();
    }

    public override void SaveFiles(string currentModId)
    {
        var list = new List<string>();
        var targetSkinDocs = _modDataDocs.Where(doc =>
            doc.GetPackageId() == currentModId &&
            string.Equals(doc.Root?.Name.LocalName, "ModInfo", StringComparison.OrdinalIgnoreCase)).ToList();
        var list2 = new List<SkinFixResult>();
        var source = CollectCompatibilityIssues();
        var list3 = source.Where(x => x.Severity == SkinCompatibilitySeverity.Blocking).ToList();
        if (list3.Count > 0)
        {
            var skinCompatibilityIssue = list3[0];
            if (MessageBox.Show(
                    "检测到皮肤兼容性阻断问题，直接导出可能导致游戏崩溃。\n\n" +
                    $"首个问题：[{skinCompatibilityIssue.SkinName}] {skinCompatibilityIssue.Message}\n\n" + "是否一键修复并继续保存？",
                    "Skin 兼容性检查", MessageBoxButton.YesNo, MessageBoxImage.Exclamation) != MessageBoxResult.Yes)
            {
                MessageBox.Show("已取消保存，请先修复阻断问题。", "Skin 保存已取消", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }
            foreach (var item in targetSkinDocs)
            {
                if (item.Root == null)
                {
                    continue;
                }
                var skinFixResult = SkinCompatibilityGuard.ApplyFix(item.Root, SkinFixPolicy.PreferPenetrateForDefault);
                if (!skinFixResult.HasChanges && skinFixResult.Messages.Count == 0 && skinFixResult.Errors.Count == 0)
                {
                    continue;
                }
                list2.Add(skinFixResult);
                foreach (var message in skinFixResult.Messages)
                {
                    Logger.Info(message);
                }
                foreach (var error in skinFixResult.Errors)
                {
                    Logger.Error(error);
                    list.Add(error);
                }
            }
            source = CollectCompatibilityIssues();
            list3 = source.Where(x => x.Severity == SkinCompatibilitySeverity.Blocking).ToList();
            if (list3.Count > 0)
            {
                var text = string.Join('\n', from x in list3.Take(5)
                    select "[" + x.SkinName + "] " + x.Message);
                MessageBox.Show("自动修复后仍存在阻断问题，已取消保存：\n" + text, "Skin 保存失败", MessageBoxButton.OK, MessageBoxImage.Hand);
                return;
            }
        }
        foreach (var item2 in source.Where(x => x.Severity == SkinCompatibilitySeverity.Warning).ToList().Take(10))
        {
            Logger.Warn("[" + item2.SkinName + "] " + item2.Message);
        }
        foreach (var modDataDoc in _modDataDocs)
        {
            if (modDataDoc.GetPackageId() != currentModId)
            {
                continue;
            }
            var text2 = modDataDoc.Root?.Annotation<FilePathAnnotation>()?.Path;
            if (!string.IsNullOrEmpty(text2))
            {
                try
                {
                    modDataDoc.Save(text2);
                }
                catch (Exception ex)
                {
                    var text3 = "保存皮肤文件失败: " + text2;
                    Logger.Error(text3, ex);
                    list.Add(text3);
                }
            }
        }
        var array = _pendingDeleteSkinDirs.ToArray();
        foreach (var text4 in array)
        {
            try
            {
                if (Directory.Exists(text4))
                {
                    Directory.Delete(text4, true);
                }
                _pendingDeleteSkinDirs.Remove(text4);
            }
            catch (Exception ex2)
            {
                var text5 = "删除皮肤目录失败: " + text4;
                Logger.Error(text5, ex2);
                list.Add(text5);
            }
        }
        array = _pendingDeleteActionImages.ToArray();
        foreach (var text6 in array)
        {
            try
            {
                if (File.Exists(text6))
                {
                    File.Delete(text6);
                }
                _pendingDeleteActionImages.Remove(text6);
            }
            catch (Exception ex3)
            {
                var text7 = "删除动作图片失败: " + text6;
                Logger.Error(text7, ex3);
                list.Add(text7);
            }
        }
        if (list.Count > 0)
        {
            var value = list.Distinct().ToArray();
            MessageBox.Show("部分皮肤文件保存/删除失败：\n" + string.Join('\n', value), "Skin 保存警告", MessageBoxButton.OK,
                MessageBoxImage.Exclamation);
        }
        else
        {
            if (list2.Count <= 0)
            {
                return;
            }
            List<string> list4 = (from x in list2
                where x.HasChanges
                select x.SkinName).Distinct().ToList();
            if (list4.Count > 0)
            {
                MessageBox.Show("已自动修复兼容问题并完成保存：\n" + string.Join('\n', list4.Select(x => "• " + x)), "Skin 兼容修复",
                    MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
        }

        List<SkinCompatibilityIssue> CollectCompatibilityIssues()
        {
            var list5 = new List<SkinCompatibilityIssue>();
            foreach (var item3 in targetSkinDocs)
            {
                if (item3.Root != null)
                {
                    list5.AddRange(SkinCompatibilityGuard.Validate(item3.Root));
                }
            }
            return list5;
        }
    }
}
