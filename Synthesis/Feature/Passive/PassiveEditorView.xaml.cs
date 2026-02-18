using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Synthesis.Core.Tools;

namespace Synthesis.Feature.Passive;

public partial class PassiveEditorView : UserControl, IRegionMemberLifetime
{
    private readonly DispatcherTimer _searchTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(400)
    };

    public PassiveEditorView()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplySorting();
        _searchTimer.Tick += (_, _) =>
        {
            _searchTimer.Stop();
            ApplyFilter();
        };
    }

    public bool KeepAlive => true;

    private void ApplySorting()
    {
        if (PassiveListBox.ItemsSource != null)
        {
            ViewSortHelper.ApplyModFirstNaturalSort<UnifiedPassive>(PassiveListBox.ItemsSource, x => x.Id);
        }
    }

    private void ApplyFilter()
    {
        if (PassiveListBox.ItemsSource == null)
        {
            return;
        }

        var view = CollectionViewSource.GetDefaultView(PassiveListBox.ItemsSource);
        if (view == null)
        {
            return;
        }

        var filterText = SearchBox.Text;
        view.Filter = obj =>
        {
            if (string.IsNullOrEmpty(filterText))
            {
                return true;
            }

            return obj is UnifiedPassive passive &&
                   (passive.Id.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                    passive.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase));
        };
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }
}
