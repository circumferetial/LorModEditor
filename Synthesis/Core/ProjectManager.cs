using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Synthesis.Core.Abstraction;
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
using Synthesis.Feature.SkinEditor;
using Synthesis.Feature.Stage;

namespace Synthesis.Core;

public class ProjectManager
{
    private static readonly IReadOnlyDictionary<Type, VanillaResourceKind> VanillaLoadStrategies =
        new Dictionary<Type, VanillaResourceKind>
        {
            [typeof(AbilityRepository)] = VanillaResourceKind.Loc,
            [typeof(KeywordRepository)] = VanillaResourceKind.Loc,
            [typeof(DialogRepository)] = VanillaResourceKind.Loc,
            [typeof(EtcRepository)] = VanillaResourceKind.Loc,
            [typeof(DropBookRepository)] = VanillaResourceKind.Data,
            [typeof(CardDropRepository)] = VanillaResourceKind.Data,
            [typeof(SkinRepository)] = VanillaResourceKind.Data,
            [typeof(CardRepository)] = VanillaResourceKind.All,
            [typeof(BookRepository)] = VanillaResourceKind.All,
            [typeof(PassiveRepository)] = VanillaResourceKind.All,
            [typeof(EnemyRepository)] = VanillaResourceKind.All,
            [typeof(StageRepository)] = VanillaResourceKind.All
        };

    private readonly SemaphoreSlim _openProjectGate = new(1, 1);

    private readonly List<IGameRepository> _repositories = [];

    private readonly Task _vanillaLoadTask;

    private bool _isVanillaLoaded;
    private bool? _lastContainOriginal;
    private string? _lastParsedLanguage;
    private string? _lastParsedProjectRoot;

    private string? _loadedVanillaPath;

    private string lang = string.Empty;

    public ProjectManager()
    {
        InitializeRepositories();
        DropBookRepo.EtcRepo = EtcRepo;
        SetSpriteResolver();
        _vanillaLoadTask = LoadVanillaAsync();
    }

    public string Location { get; } = Directory.GetCurrentDirectory();

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

    public SkinRepository SkinRepo { get; } = new();

    public string? ProjectRootPath { get; private set; }

    public string CurrentModId { get; private set; } = "MyMod";

    public ObservableCollection<string> Artworks { get; } = [];

    public Dictionary<string, string> ArtworkPathDict { get; } = new();

    public string CurrentLanguage => NormalizeLanguage(lang);

    private static string NormalizeLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "cn";
        }

        return language.Trim();
    }

    private void InitializeRepositories()
    {
        _repositories.Add(KeywordRepo);
        _repositories.Add(SkinRepo);
        _repositories.Add(EtcRepo);
        _repositories.Add(DialogRepo);
        _repositories.Add(AbilityRepo);
        _repositories.Add(PassiveRepo);
        _repositories.Add(CardRepo);
        _repositories.Add(BookRepo);
        _repositories.Add(EnemyRepo);
        _repositories.Add(StageRepo);
        _repositories.Add(DropBookRepo);
        _repositories.Add(CardDropRepo);
    }

    private static VanillaResourceKind ResolveVanillaLoadStrategy(IGameRepository repository) =>
        VanillaLoadStrategies.GetValueOrDefault(repository.GetType(), VanillaResourceKind.All);

    private static void LoadVanillaResources(IGameRepository repository, string basePath, string language)
    {
        switch (ResolveVanillaLoadStrategy(repository))
        {
            case VanillaResourceKind.None:
                break;
            case VanillaResourceKind.Data:
                if (repository is IVanillaDataLoadable vanillaDataLoadable)
                {
                    vanillaDataLoadable.LoadDataResources(basePath, "@origin");
                }
                break;
            case VanillaResourceKind.Loc:
                repository.LoadLocResources(basePath, language, "@origin");
                break;
            default:
                repository.LoadResources(basePath, language, "@origin");
                break;
        }
    }

    private async Task LoadVanillaAsync()
    {
        var instance = EditorConfig.Instance;
        var basePath = instance.BaseModPath;
        if (string.IsNullOrEmpty(basePath) || !Directory.Exists(basePath) || _isVanillaLoaded &&
            string.Equals(_loadedVanillaPath, basePath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        lang = NormalizeLanguage(instance.Language);
        await Task.Run(delegate
        {
            try
            {
                foreach (var repository in _repositories)
                {
                    LoadVanillaResources(repository, basePath, lang);
                }
                _isVanillaLoaded = true;
                _loadedVanillaPath = basePath;
            }
            catch (Exception ex)
            {
                var ex2 = ex;
                var ex3 = ex2;
                Application.Current.Dispatcher.Invoke((Func<MessageBoxResult>)(() =>
                    MessageBox.Show("原版资源加载失败: " + ex3.Message)));
            }
        });
    }

    public async Task OpenProject(string stageModInfoPath)
    {
        await _openProjectGate.WaitAsync();
        try
        {
            var settings = EditorConfig.Instance;
            var requestedLanguage = NormalizeLanguage(settings.Language);
            var directoryName = Path.GetDirectoryName(stageModInfoPath);
            var isSameProject =
                string.Equals(ProjectRootPath, directoryName, StringComparison.OrdinalIgnoreCase);
            var languageChanged =
                !string.Equals(CurrentLanguage, requestedLanguage, StringComparison.OrdinalIgnoreCase);
            var refreshNeeded = NeedsRepositoryRefresh();

            if (settings.ShowVanillaData && (!isSameProject || languageChanged || refreshNeeded))
            {
                await _vanillaLoadTask;
            }

            if (isSameProject)
            {
                if (languageChanged)
                {
                    await SwitchLanguageAsync(requestedLanguage);
                }
                RefreshAllRepositories();
                MessageBox.Show("项目加载完成: " + CurrentModId, "就绪");
                return;
            }
            ProjectRootPath = directoryName ?? string.Empty;
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
            ClearModOnly();
            await Task.Run(delegate
            {
                lang = requestedLanguage;
                foreach (var repository in _repositories)
                {
                    repository.LoadResources(ProjectRootPath, lang, CurrentModId);
                    repository.EnsureDefaults(ProjectRootPath, lang, CurrentModId);
                }
                ScriptScanner.Scan(ProjectRootPath);
            });
            RefreshAllRepositories(true);
            ScanArtworks(ProjectRootPath);
            MessageBox.Show("项目加载完成: " + CurrentModId, "就绪");
        }
        catch (Exception ex)
        {
            MessageBox.Show("项目加载失败：\n" + ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Hand);
        }
        finally
        {
            _openProjectGate.Release();
        }
    }

    public async Task SwitchLanguageAsync(string newLanguage)
    {
        newLanguage = NormalizeLanguage(newLanguage);
        if (string.Equals(CurrentLanguage, newLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        EditorConfig.Instance.Language = newLanguage;
        EditorConfig.Instance.Save();
        await Task.Run(delegate
        {
            foreach (var repository in _repositories)
            {
                repository.ClearLocOnly();
            }
            if (_isVanillaLoaded && !string.IsNullOrEmpty(_loadedVanillaPath))
            {
                foreach (var repository2 in _repositories)
                {
                    repository2.LoadLocResources(_loadedVanillaPath, newLanguage, "@origin");
                }
            }
            if (!string.IsNullOrEmpty(ProjectRootPath) && Directory.Exists(ProjectRootPath))
            {
                foreach (var repository3 in _repositories)
                {
                    repository3.LoadLocResources(ProjectRootPath, newLanguage, CurrentModId);
                }
            }
        });
        lang = newLanguage;
        RefreshAllRepositories(true);
        if (!string.IsNullOrEmpty(ProjectRootPath))
        {
            ScanArtworks(ProjectRootPath);
        }
        MessageBox.Show("语言已切换至 " + newLanguage, "提示");
    }

    private void RefreshAllRepositories(bool force = false)
    {
        if (!NeedsRepositoryRefresh(force))
        {
            return;
        }

        var containOriginal = EditorConfig.Instance.ShowVanillaData;
        var currentRoot = ProjectRootPath ?? string.Empty;
        var currentLanguage = CurrentLanguage;

        Application.Current.Dispatcher.Invoke(delegate
        {
            foreach (var repository in _repositories)
            {
                repository.Parse(containOriginal);
            }
        });

        _lastContainOriginal = containOriginal;
        _lastParsedProjectRoot = currentRoot;
        _lastParsedLanguage = currentLanguage;
    }

    private bool NeedsRepositoryRefresh(bool force = false)
    {
        if (force)
        {
            return true;
        }

        var containOriginal = EditorConfig.Instance.ShowVanillaData;
        var currentRoot = ProjectRootPath ?? string.Empty;
        var currentLanguage = CurrentLanguage;

        return _lastContainOriginal != containOriginal ||
               !string.Equals(_lastParsedProjectRoot, currentRoot, StringComparison.OrdinalIgnoreCase) ||
               !string.Equals(_lastParsedLanguage, currentLanguage, StringComparison.OrdinalIgnoreCase);
    }

    private void ClearModOnly()
    {
        foreach (var repository in _repositories)
        {
            repository.ClearModOnly();
        }
        Artworks.Clear();
        ArtworkPathDict.Clear();
    }

    public void SaveAll()
    {
        foreach (var repository in _repositories)
        {
            repository.SaveFiles(CurrentModId);
        }
    }

    public void ClearAll()
    {
        foreach (var repository in _repositories)
        {
            repository.ClearAll();
        }
        _isVanillaLoaded = false;
        _loadedVanillaPath = null;
        ProjectRootPath = null;
        CurrentModId = "MyMod";
        lang = string.Empty;
        _lastContainOriginal = null;
        _lastParsedProjectRoot = null;
        _lastParsedLanguage = null;
        Artworks.Clear();
        ArtworkPathDict.Clear();
    }

    private void SetSpriteResolver()
    {
        UnityRichTextHelper.SpriteResolver = delegate(string s)
        {
            var artworkPath = GetArtworkPath(s);
            if (artworkPath == null || !File.Exists(artworkPath))
            {
                return null;
            }
            try
            {
                var buffer = File.ReadAllBytes(artworkPath);
                var bitmapImage = new BitmapImage();
                using var streamSource = new MemoryStream(buffer);
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = streamSource;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
            catch
            {
                return null;
            }
        };
    }

    private void ScanArtworks(string rootDir)
    {
        Artworks.Clear();
        ArtworkPathDict.Clear();
        var obj = new string[2]
        {
            Path.Combine(rootDir, "Artwork"),
            Path.Combine(Location, "Resource", "TmpIcons")
        };
        var hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var array = obj;
        foreach (var path in array)
        {
            if (!Directory.Exists(path))
            {
                continue;
            }
            foreach (var item in Directory.EnumerateFiles(path, "*.png", SearchOption.AllDirectories))
            {
                var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item);
                if (!string.IsNullOrWhiteSpace(fileNameWithoutExtension) && hashSet.Add(fileNameWithoutExtension))
                {
                    ArtworkPathDict[fileNameWithoutExtension] = item;
                }
            }
        }
        foreach (var item2 in ArtworkPathDict.Keys.OrderBy(x => x))
        {
            Artworks.Add(item2);
        }
    }

    public string? GetArtworkPath(string name) => ArtworkPathDict.GetValueOrDefault(name);
}
