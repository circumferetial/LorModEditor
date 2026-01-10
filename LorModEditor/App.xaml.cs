using System.Windows;
using System.Windows.Threading;
using LorModEditor.Core;
using LorModEditor.Core.Log;
using LorModEditor.Views;

namespace LorModEditor;

public partial class App : PrismApplication
{
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<ProjectManager>();

        // 【核心】注册导航视图
        // 只要注册了，Prism 就能通过名字找到它们
        containerRegistry.RegisterForNavigation<CardEditorView>();
        containerRegistry.RegisterForNavigation<PassiveEditorView>();
        containerRegistry.RegisterForNavigation<BookEditorView>();
        containerRegistry.RegisterForNavigation<EnemyEditorView>();
        containerRegistry.RegisterForNavigation<StageEditorView>();
        containerRegistry.RegisterForNavigation<DropBookEditorView>();
        containerRegistry.RegisterForNavigation<AbilityEditorView>();
        containerRegistry.RegisterForNavigation<SettingsView>();
        containerRegistry.RegisterForNavigation<KeywordEditorView>();
    }

    protected override Window CreateShell() =>
        // Container.Resolve 会自动帮你把依赖项注入进去
        Container.Resolve<MainWindow>();

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. 捕获 UI 线程的未处理异常
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        // 2. 捕获后台线程 (Task) 的未处理异常
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        // 3. 捕获非托管异常 (兜底)
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        Logger.Info("Application Startup.");
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Error("UI Thread Crash!", e.Exception);
        MessageBox.Show($"程序遇到严重错误：\n{e.Exception.Message}\n\n详情请查看 latest.log", "崩溃", MessageBoxButton.OK,
            MessageBoxImage.Error);

        // 设置为 true 表示“我处理了”，防止程序立即闪退（视情况而定，严重错误最好还是让它退）
        e.Handled = true;
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Logger.Error("Background Task Crash!", e.Exception);
        e.SetObserved();// 标记为已观察，防止崩溃
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            Logger.Error("Critical System Crash!", ex);
            MessageBox.Show("发生致命错误，程序即将退出。", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Info("Application Exit.");
        base.OnExit(e);
    }
}