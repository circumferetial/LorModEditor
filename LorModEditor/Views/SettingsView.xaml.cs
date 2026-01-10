namespace LorModEditor.Views;

public partial class SettingsView : IRegionMemberLifetime
{
    public SettingsView()
    {
        InitializeComponent();
    }

    public bool KeepAlive => true;
}