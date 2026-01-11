using System.IO;
using System.Windows;
using LorModEditor.Core;
using LorModEditor.Views;

namespace LorModEditor;

public partial class App
{
    protected override Window CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 核心单例
        containerRegistry.RegisterSingleton<ProjectManager>();

        // 注册内置导航视图
        containerRegistry.RegisterForNavigation<CardEditorView>();
        containerRegistry.RegisterForNavigation<PassiveEditorView>();
        containerRegistry.RegisterForNavigation<BookEditorView>();
        containerRegistry.RegisterForNavigation<EnemyEditorView>();
        containerRegistry.RegisterForNavigation<StageEditorView>();
        containerRegistry.RegisterForNavigation<DropBookEditorView>();
        containerRegistry.RegisterForNavigation<AbilityEditorView>();
        containerRegistry.RegisterForNavigation<KeywordEditorView>();
        containerRegistry.RegisterForNavigation<SettingsView>();
    }

    // =========================================================
    // 【核心修正】创建目录时就决定加载策略
    // =========================================================
    protected override IModuleCatalog CreateModuleCatalog()
    {
        // 1. 创建一个目录
        var catalog = new DirectoryModuleCatalog();
            
        // 2. 指定 Plugins 文件夹路径
        var pluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        if (!Directory.Exists(pluginPath))
            Directory.CreateDirectory(pluginPath);
            
        catalog.ModulePath = pluginPath;

        return catalog;
    }

    // =========================================================
    // 如果你将来要把内置功能拆成 Module (比如 CardModule)，
    // 你可以在这里 AddModule。但目前你是单体架构，这里可以留空。
    // =========================================================
    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        // 如果以后有内置模块，这样写：
        // moduleCatalog.AddModule<LorModEditor.Modules.Card.CardModule>();
            
        // 因为我们在 CreateModuleCatalog 里已经返回了 DirectoryModuleCatalog，
        // Prism 会自动扫描那个文件夹。
        base.ConfigureModuleCatalog(moduleCatalog);
    }
}