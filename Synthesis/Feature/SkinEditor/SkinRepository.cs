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

    private static string EnsureTrailingSeparator(string path) =>
        path.EndsWith(Path.DirectorySeparatorChar) || path.EndsWith(Path.AltDirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;

    private static string? GetDocPath(XDocument doc) => doc.Root?.Annotation<FilePathAnnotation>()?.Path;

    public override void LoadResources(string root, string lang, string modId) => LoadDataResources(root, modId);

    public override void LoadDataResources(string projectRoot, string modId)
    {
        var skinRoot = Path.Combine(projectRoot, "Resource", "CharacterSkin");
        if (!Directory.Exists(skinRoot)) return;

        foreach (var dir in Directory.GetDirectories(skinRoot))
        {
            var modInfoPath = Path.Combine(dir, "ModInfo.xml");
            if (!File.Exists(modInfoPath)) continue;

            try
            {
                var doc = XDocument.Load(modInfoPath);
                if (doc.Root?.Name.LocalName != "ModInfo") continue;

                doc.Root.AddAnnotation("PID:" + modId);
                doc.Root.AddAnnotation(new FilePathAnnotation(modInfoPath));
                AddDataDoc(doc);// 会按 IsVanilla 自动进 vanilla/mod 桶
            }
            catch (Exception ex)
            {
                Logger.Error("SkinLoad", ex);
            }
        }
    }

    public override void EnsureDefaults(string root, string lang, string modId)
    {
        // Skin 不强制创建默认文件
    }

    public override void Parse(bool containOriginal)
    {
        Items.Clear();

        var dataDocs = containOriginal ? _dataDocs : _modDataDocs;
        foreach (var doc in dataDocs)
        {
            if (doc.Root?.Name.LocalName != "ModInfo") continue;
            Items.Add(new UnifiedSkin(doc.Root));
        }
    }

    // 创建新皮肤（只写入 Mod）
    public UnifiedSkin? Create(string projectRoot, string skinName, string modId)
    {
        var skinDir = Path.Combine(projectRoot, "Resource", "CharacterSkin", skinName);
        if (!Directory.Exists(skinDir)) Directory.CreateDirectory(skinDir);

        var xmlPath = Path.Combine(skinDir, "ModInfo.xml");
        if (File.Exists(xmlPath)) return null;

        var doc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), new XElement("ModInfo"));
        var clothInfo = new XElement("ClothInfo");
        clothInfo.Add(new XElement("Name", skinName));
        clothInfo.Add(SkinCompatibilityGuard.CreateStandardActionNode(ActionDetail.Default.ToString()));
        doc.Root!.Add(clothInfo);

        doc.Save(xmlPath);

        doc.Root.AddAnnotation("PID:" + modId);
        doc.Root.AddAnnotation(new FilePathAnnotation(xmlPath));

        AddDataDoc(doc);// 进 mod 桶
        var unifiedSkin = new UnifiedSkin(doc.Root);
        Items.Add(unifiedSkin);
        return unifiedSkin;
    }

    public override void Delete(UnifiedSkin item)
    {
        if (item.IsVanilla) return;

        if (item.Element.Document != null)
            _modDataDocs.Remove(item.Element.Document);

        var itemDocPath = item.Element.Document?.Root?.Annotation<FilePathAnnotation>()?.Path;
        var normalizedDocPath = string.IsNullOrWhiteSpace(itemDocPath) ? null : NormalizePath(itemDocPath);

        if (normalizedDocPath != null)
        {
            _modDataDocs.RemoveAll(doc =>
            {
                var path = GetDocPath(doc);
                return !string.IsNullOrWhiteSpace(path) &&
                       string.Equals(NormalizePath(path), normalizedDocPath, StringComparison.OrdinalIgnoreCase);
            });

            var skinDir = Path.GetDirectoryName(normalizedDocPath);
            if (!string.IsNullOrWhiteSpace(skinDir))
            {
                var normalizedSkinDir = NormalizePath(skinDir);
                _pendingDeleteSkinDirs.Add(normalizedSkinDir);

                var folderPrefix = EnsureTrailingSeparator(normalizedSkinDir);
                _pendingDeleteActionImages.RemoveWhere(path =>
                    NormalizePath(path).StartsWith(folderPrefix, StringComparison.OrdinalIgnoreCase));
            }
        }

        base.Delete(item);
        NotifyStatusChanged();
    }

    public void QueueActionImageDeletion(string projectRoot, string skinName, string actionName)
    {
        if (string.IsNullOrWhiteSpace(projectRoot) ||
            string.IsNullOrWhiteSpace(skinName) ||
            string.IsNullOrWhiteSpace(actionName)) return;

        var imagePath = Path.Combine(projectRoot, "Resource", "CharacterSkin", skinName, "ClothCustom",
            $"{actionName}.png");
        _pendingDeleteActionImages.Add(NormalizePath(imagePath));
    }

    public void CancelActionImageDeletion(string projectRoot, string skinName, string actionName)
    {
        if (string.IsNullOrWhiteSpace(projectRoot) ||
            string.IsNullOrWhiteSpace(skinName) ||
            string.IsNullOrWhiteSpace(actionName)) return;

        var imagePath = Path.Combine(projectRoot, "Resource", "CharacterSkin", skinName, "ClothCustom",
            $"{actionName}.png");
        _pendingDeleteActionImages.Remove(NormalizePath(imagePath));
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
        var errors = new List<string>();
        var targetSkinDocs = _modDataDocs
            .Where(doc => doc.GetPackageId() == currentModId &&
                          string.Equals(doc.Root?.Name.LocalName, "ModInfo", StringComparison.OrdinalIgnoreCase))
            .ToList();

        List<SkinCompatibilityIssue> CollectCompatibilityIssues()
        {
            var list = new List<SkinCompatibilityIssue>();
            foreach (var doc in targetSkinDocs)
            {
                if (doc.Root == null) continue;
                list.AddRange(SkinCompatibilityGuard.Validate(doc.Root));
            }

            return list;
        }

        var fixResults = new List<SkinFixResult>();
        var compatibilityIssues = CollectCompatibilityIssues();
        var blockingIssues = compatibilityIssues
            .Where(x => x.Severity == SkinCompatibilitySeverity.Blocking)
            .ToList();

        if (blockingIssues.Count > 0)
        {
            var firstBlocking = blockingIssues[0];
            var askFix = MessageBox.Show(
                "检测到皮肤兼容性阻断问题，直接导出可能导致游戏崩溃。\n\n" +
                $"首个问题：[{firstBlocking.SkinName}] {firstBlocking.Message}\n\n" +
                "是否一键修复并继续保存？",
                "Skin 兼容性检查",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (askFix != MessageBoxResult.Yes)
            {
                MessageBox.Show("已取消保存，请先修复阻断问题。", "Skin 保存已取消",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            foreach (var doc in targetSkinDocs)
            {
                if (doc.Root == null) continue;

                var fixResult = SkinCompatibilityGuard.ApplyFix(doc.Root, SkinFixPolicy.PreferPenetrateForDefault);
                if (!fixResult.HasChanges && fixResult.Messages.Count == 0 && fixResult.Errors.Count == 0) continue;

                fixResults.Add(fixResult);

                foreach (var msg in fixResult.Messages) Logger.Info(msg);
                foreach (var err in fixResult.Errors)
                {
                    Logger.Error(err);
                    errors.Add(err);
                }
            }

            compatibilityIssues = CollectCompatibilityIssues();
            blockingIssues = compatibilityIssues
                .Where(x => x.Severity == SkinCompatibilitySeverity.Blocking)
                .ToList();

            if (blockingIssues.Count > 0)
            {
                var topBlocking = string.Join('\n',
                    blockingIssues.Take(5).Select(x => $"[{x.SkinName}] {x.Message}"));
                MessageBox.Show(
                    "自动修复后仍存在阻断问题，已取消保存：\n" + topBlocking,
                    "Skin 保存失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
        }

        var warningIssues = compatibilityIssues
            .Where(x => x.Severity == SkinCompatibilitySeverity.Warning)
            .ToList();
        foreach (var warning in warningIssues.Take(10))
            Logger.Warn($"[{warning.SkinName}] {warning.Message}");

        // 1) 先保存仍然存在的 mod 文件
        foreach (var doc in _modDataDocs)
        {
            if (doc.GetPackageId() != currentModId) continue;

            var path = doc.Root?.Annotation<FilePathAnnotation>()?.Path;
            if (string.IsNullOrEmpty(path)) continue;

            try
            {
                doc.Save(path);
            }
            catch (Exception ex)
            {
                var msg = $"保存皮肤文件失败: {path}";
                Logger.Error(msg, ex);
                errors.Add(msg);
            }
        }

        // 2) 保存后执行“整皮肤目录”删除
        foreach (var skinDir in _pendingDeleteSkinDirs.ToArray())
        {
            try
            {
                if (Directory.Exists(skinDir))
                    Directory.Delete(skinDir, true);

                _pendingDeleteSkinDirs.Remove(skinDir);
            }
            catch (Exception ex)
            {
                var msg = $"删除皮肤目录失败: {skinDir}";
                Logger.Error(msg, ex);
                errors.Add(msg);
            }
        }

        // 3) 最后执行“动作图片”删除（文件不存在视为成功）
        foreach (var imagePath in _pendingDeleteActionImages.ToArray())
        {
            try
            {
                if (File.Exists(imagePath))
                    File.Delete(imagePath);

                _pendingDeleteActionImages.Remove(imagePath);
            }
            catch (Exception ex)
            {
                var msg = $"删除动作图片失败: {imagePath}";
                Logger.Error(msg, ex);
                errors.Add(msg);
            }
        }

        if (errors.Count > 0)
        {
            var distinctErrors = errors.Distinct().ToArray();
            MessageBox.Show("部分皮肤文件保存/删除失败：\n" + string.Join('\n', distinctErrors), "Skin 保存警告",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (fixResults.Count > 0)
        {
            var fixedSkins = fixResults
                .Where(x => x.HasChanges)
                .Select(x => x.SkinName)
                .Distinct()
                .ToList();

            if (fixedSkins.Count > 0)
            {
                MessageBox.Show(
                    "已自动修复兼容问题并完成保存：\n" +
                    string.Join('\n', fixedSkins.Select(x => $"• {x}")),
                    "Skin 兼容修复",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
    }
}
