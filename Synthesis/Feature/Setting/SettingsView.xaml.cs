using System.Windows.Controls;

namespace Synthesis.Feature.Setting;

public partial class SettingsView : UserControl, IRegionMemberLifetime
{
    public SettingsView()
    {
        InitializeComponent();
    }

    public bool KeepAlive => true;
}
