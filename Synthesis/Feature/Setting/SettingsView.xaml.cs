namespace Synthesis.Feature.Setting;

public partial class SettingsView : IRegionMemberLifetime
{
    public SettingsView()
    {
        InitializeComponent();
    }

    public bool KeepAlive => true;
}