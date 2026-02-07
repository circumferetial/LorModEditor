using Synthesis.Plugins.Backup.Views;

namespace Synthesis.Plugins.Backup;

public class BackupModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider)
    {
        var regionManager = containerProvider.Resolve<IRegionManager>();

        // 【关键】把按钮塞进主程序的 ToolbarRegion
        regionManager.RegisterViewWithRegion("ToolbarRegion", typeof(BackupButton));
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 如果有特殊服务在这里注册
    }
}