using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Synthesis.Core.Tools;

namespace Synthesis.Feature.Ability;

public partial class AbilityEditorView : UserControl, IRegionMemberLifetime
{
    private readonly DispatcherTimer _searchTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(400)
    };

    public AbilityEditorView()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplySorting();
        _searchTimer.Tick += SearchTimer_Tick;
    }

    public bool KeepAlive => true;

    private void ApplySorting()
    {
        if (AbilityListBox.ItemsSource != null)
        {
            ViewSortHelper.ApplyModFirstNaturalSort<UnifiedAbility>(AbilityListBox.ItemsSource, x => x.Id);
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private void SearchTimer_Tick(object? sender, EventArgs e)
    {
        _searchTimer.Stop();
        var filterText = SearchBox.Text;
        var view = CollectionViewSource.GetDefaultView(AbilityListBox.ItemsSource);
        if (view == null)
        {
            return;
        }

        view.Filter = obj =>
        {
            if (string.IsNullOrEmpty(filterText))
            {
                return true;
            }

            return obj is UnifiedAbility ability &&
                   (ability.Id.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                    ability.Desc.Contains(filterText, StringComparison.OrdinalIgnoreCase));
        };
    }

    private void AbilityListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        EditPanel.Visibility = AbilityListBox.SelectedItem == null ? Visibility.Hidden : Visibility.Visible;
    }
}
