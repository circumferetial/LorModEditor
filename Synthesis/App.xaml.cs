using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Synthesis.Core;
using Synthesis.Core.Log;
using Synthesis.Feature.Ability;
using Synthesis.Feature.Book;
using Synthesis.Feature.Card;
using Synthesis.Feature.DropBook;
using Synthesis.Feature.Enemy;
using Synthesis.Feature.Keyword;
using Synthesis.Feature.MainWindow;
using Synthesis.Feature.Passive;
using Synthesis.Feature.Setting;
using Synthesis.Feature.SkinEditor;
using Synthesis.Feature.Stage;

namespace Synthesis;

public partial class App : PrismApplication
{
    private static bool _pluginLoadErrorShown;

    public App()
    {
        PresentationTraceSources.Refresh();
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        Logger.Error($"UI线程未处理异常 sender: {sender}", args.Exception);
        if (!_pluginLoadErrorShown && IsPluginLoadFailure(args.Exception))
        {
            _pluginLoadErrorShown = true;
            MessageBox.Show(
                "插件加载失败，已记录日志，请检查 Plugins 目录和 latest.log",
                "插件加载失败",
                MessageBoxButton.OK,
                MessageBoxImage.Exclamation);
        }

        args.Handled = true;
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        Logger.Error($"非UI线程未处理异常 sender: {sender}", args.ExceptionObject as Exception);
    }

    private static bool IsPluginLoadFailure(Exception? exception)
    {
        while (exception != null)
        {
            var typeName = exception.GetType().FullName ?? string.Empty;
            var message = exception.Message;
            var stackTrace = exception.StackTrace ?? string.Empty;
            if (typeName.Contains("Prism.Modularity", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("Prism.Modularity", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("插件", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("DirectoryModuleCatalog", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("module was trying to be loaded", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("Could not load file or assembly", StringComparison.OrdinalIgnoreCase) &&
                message.Contains("Plugin", StringComparison.OrdinalIgnoreCase) ||
                stackTrace.Contains("Prism.Modularity", StringComparison.OrdinalIgnoreCase) ||
                stackTrace.Contains("Plugins", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            exception = exception.InnerException;
        }

        return false;
    }

    protected override Window CreateShell() => Container.Resolve<MainWindow>();

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<ProjectManager>();
        containerRegistry.RegisterForNavigation<CardEditorView>();
        containerRegistry.RegisterForNavigation<PassiveEditorView>();
        containerRegistry.RegisterForNavigation<BookEditorView>();
        containerRegistry.RegisterForNavigation<EnemyEditorView>();
        containerRegistry.RegisterForNavigation<StageEditorView>();
        containerRegistry.RegisterForNavigation<DropBookEditorView>();
        containerRegistry.RegisterForNavigation<AbilityEditorView>();
        containerRegistry.RegisterForNavigation<KeywordEditorView>();
        containerRegistry.RegisterForNavigation<SettingsView>();
        containerRegistry.RegisterForNavigation<SkinEditorView>();
    }

    protected override IModuleCatalog CreateModuleCatalog()
    {
        var moduleCatalog = new DirectoryModuleCatalog();
        var pluginDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins");
        if (!Directory.Exists(pluginDirectory))
        {
            Directory.CreateDirectory(pluginDirectory);
        }

        moduleCatalog.ModulePath = pluginDirectory;
        return moduleCatalog;
    }

    protected override void OnInitialized()
    {
        try
        {
            base.OnInitialized();
            Container.Resolve<ProjectManager>();
        }
        catch (Exception ex)
        {
            MessageBox.Show("原版预加载失败：\n" + ex.Message, "提示");
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        var flushTask = Logger.FlushAsync();
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(2));
        var completedTask = Task.WhenAny(flushTask, timeoutTask).GetAwaiter().GetResult();
        if (completedTask != flushTask)
        {
            Debug.WriteLine("Logger flush on exit timed out.");
        }
        else if (flushTask.IsFaulted)
        {
            Debug.WriteLine($"Logger flush on exit failed: {flushTask.Exception}");
        }

        base.OnExit(e);
    }
}
