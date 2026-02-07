using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Xml.Linq;
using Synthesis.Core.Tools;
using Synthesis.Feature.Ability;
using Synthesis.Feature.Book;
using Synthesis.Feature.Card;
using Synthesis.Feature.Dialog;
using Synthesis.Feature.DropBook;
using Synthesis.Feature.DropBook.CardDrop;
using Synthesis.Feature.Enemy;
using Synthesis.Feature.Keyword;
using Synthesis.Feature.Passive;
using Synthesis.Feature.Setting;
using Synthesis.Feature.OldSkinEditor;
using Synthesis.Feature.Stage;

namespace Synthesis.Core;

public class ProjectManager
{
    public ProjectManager()
    {
        // 注入依赖
        DropBookRepo.EtcRepo = EtcRepo;
    }

    // 仓库
    public CardRepository CardRepo { get; } = new();
    public PassiveRepository PassiveRepo { get; } = new();
    public AbilityRepository AbilityRepo { get; } = new();
    public BookRepository BookRepo { get; } = new();
    public EnemyRepository EnemyRepo { get; } = new();
    public StageRepository StageRepo { get; } = new();
    public DropBookRepository DropBookRepo { get; } = new();
    public CardDropRepository CardDropRepo { get; } = new();
    public KeywordRepository KeywordRepo { get; } = new();
    public EtcRepository EtcRepo { get; } = new();
    public DialogRepository DialogRepo { get; } = new();
    public ScriptScanner ScriptScanner { get; } = new();

    public string? ProjectRootPath { get; private set; }
    public string? BaseModPath { get; private set; }
    public string CurrentModId { get; private set; } = "MyMod";

    public ObservableCollection<string> Artworks { get; } = new();
    public Dictionary<string, string> ArtworkPathDict { get; } = new();
    public SkinRepository SkinRepo { get; } = new SkinRepository();

    public Synthesis.Feature.SkinEditor.SkinRepository NewSkinRepo { get; set; } =
        new Feature.SkinEditor.SkinRepository();

    public async Task OpenProject(string stageModInfoPath)
    {
        try
        {
            ClearAll();
            ProjectRootPath = Path.GetDirectoryName(stageModInfoPath) ?? string.Empty;

            try
            {
                CurrentModId = XDocument.Load(stageModInfoPath).Descendants("ID").FirstOrDefault()?.Value ?? "MyMod";
            }
            catch
            {
                CurrentModId = "MyMod";
            }

            EditorConfig.Instance.LastProjectPath = stageModInfoPath;
            EditorConfig.Instance.Save();

            await Task.Run(() =>
            {
                var lang = EditorConfig.Instance.Language;

                // 1. 加载 BaseMod (原版)
                if (EditorConfig.Instance.LoadVanillaData)
                {
                    BaseModPath = EditorConfig.Instance.BaseModPath;
                    if (string.IsNullOrEmpty(BaseModPath)) BaseModPath = GamePathService.GetBaseModPath();

                    if (!string.IsNullOrEmpty(BaseModPath) && Directory.Exists(BaseModPath))
                    {
                        if (EditorConfig.Instance.BaseModPath != BaseModPath)
                        {
                            EditorConfig.Instance.BaseModPath = BaseModPath;
                            EditorConfig.Instance.Save();
                        }
                        // 委托所有仓库加载原版
                        LoadAllRepositories(BaseModPath, lang, LorId.Vanilla);
                    }
                }

                // 2. 加载用户 Mod
                LoadAllRepositories(ProjectRootPath, lang, CurrentModId);

                // 3. 查漏补缺 (委托仓库创建模板)
                EnsureDefaults(ProjectRootPath, lang, CurrentModId);
            });

            // 4. 扫描资源
            ScanArtworks(ProjectRootPath);
            ScriptScanner.Scan(ProjectRootPath);
            // 5. 解析数据
            ParseAllRepositories();

            MessageBox.Show("项目加载完成", "就绪");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"项目加载失败：\n{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // --- 委托分发 ---
    private void LoadAllRepositories(string root, string lang, string modId)
    {
        CardRepo.LoadResources(root, lang, modId);
        PassiveRepo.LoadResources(root, lang, modId);
        BookRepo.LoadResources(root, lang, modId);
        EnemyRepo.LoadResources(root, lang, modId);
        StageRepo.LoadResources(root, lang, modId);
        DropBookRepo.LoadResources(root, lang, modId);
        CardDropRepo.LoadResources(root, lang, modId);
        AbilityRepo.LoadResources(root, lang, modId);
        KeywordRepo.LoadResources(root, lang, modId);
        EtcRepo.LoadResources(root, lang, modId);
        DialogRepo.LoadResources(root, lang, modId);
        SkinRepo.LoadResources(root, lang, modId);
    }

    private void EnsureDefaults(string root, string lang, string modId)
    {
        CardRepo.EnsureDefaults(root, lang, modId);
        PassiveRepo.EnsureDefaults(root, lang, modId);
        BookRepo.EnsureDefaults(root, lang, modId);
        EnemyRepo.EnsureDefaults(root, lang, modId);
        StageRepo.EnsureDefaults(root, lang, modId);
        DropBookRepo.EnsureDefaults(root, lang, modId);
        CardDropRepo.EnsureDefaults(root, lang, modId);
        AbilityRepo.EnsureDefaults(root, lang, modId);
        KeywordRepo.EnsureDefaults(root, lang, modId);
        EtcRepo.EnsureDefaults(root, lang, modId);
        DialogRepo.EnsureDefaults(root, lang, modId);
        SkinRepo.EnsureDefaults(root, lang, modId);
    }

    private void ParseAllRepositories()
    {
        CardRepo.Load();
        PassiveRepo.Load();
        AbilityRepo.Load();
        BookRepo.Load();
        EnemyRepo.Load();
        StageRepo.Load();
        DropBookRepo.Load();
        CardDropRepo.Load();
        KeywordRepo.Load();
        EtcRepo.Load();
        DialogRepo.Load();
        SkinRepo.Load();
    }

    private void ClearAll()
    {
        CardRepo.Clear();
        PassiveRepo.Clear();
        AbilityRepo.Clear();
        BookRepo.Clear();
        EnemyRepo.Clear();
        StageRepo.Clear();
        DropBookRepo.Clear();
        CardDropRepo.Clear();
        KeywordRepo.Clear();
        EtcRepo.Clear();
        Artworks.Clear();
        ArtworkPathDict.Clear();
        DialogRepo.Clear();
        SkinRepo.Clear();
    }

    public void SaveAll()
    {
        CardRepo.SaveFiles(CurrentModId);
        PassiveRepo.SaveFiles(CurrentModId);
        BookRepo.SaveFiles(CurrentModId);
        EnemyRepo.SaveFiles(CurrentModId);
        StageRepo.SaveFiles(CurrentModId);
        DropBookRepo.SaveFiles(CurrentModId);
        CardDropRepo.SaveFiles(CurrentModId);
        AbilityRepo.SaveFiles(CurrentModId);
        KeywordRepo.SaveFiles(CurrentModId);
        EtcRepo.SaveFiles(CurrentModId);
        DialogRepo.SaveFiles(CurrentModId);
        SkinRepo.SaveFiles(CurrentModId);
    }

    private void ScanArtworks(string rootDir)
    {
        Artworks.Clear();
        ArtworkPathDict.Clear();
        var dirs = new[] { Path.Combine(rootDir, "Artwork"), Path.Combine(rootDir, "Resource", "CombatPageArtwork") };


        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir)) continue;
            foreach (var file in Directory.GetFiles(dir, "*.png", SearchOption.AllDirectories))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                if (Artworks.Contains(name)) continue;
                Artworks.Add(name);
                ArtworkPathDict[name] = file;
            }
        }
    }

    public string? GetArtworkPath(string name) => ArtworkPathDict.GetValueOrDefault(name);
}