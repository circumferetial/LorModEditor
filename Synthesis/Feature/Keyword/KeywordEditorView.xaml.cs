using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Synthesis.Core.Tools;

namespace Synthesis.Feature.Keyword;

public partial class KeywordEditorView : UserControl, IRegionMemberLifetime
{
    private readonly DispatcherTimer _searchTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(400)
    };

    public KeywordEditorView()
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
        if (KeywordListBox.ItemsSource != null)
        {
            ViewSortHelper.ApplyModFirstNaturalSort<UnifiedKeyword>(KeywordListBox.ItemsSource, x => x.Id);
        }
    }

    private void ApplyFilter()
    {
        if (KeywordListBox.ItemsSource == null)
        {
            return;
        }

        var view = CollectionViewSource.GetDefaultView(KeywordListBox.ItemsSource);
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

            return obj is UnifiedKeyword keyword &&
                   (keyword.Id.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                    keyword.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                    keyword.Desc.Contains(filterText, StringComparison.OrdinalIgnoreCase));
        };
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private void KeywordListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (KeywordListBox.SelectedItem != null)
        {
            EditPanel.Visibility = Visibility.Visible;
            EditPanel.DataContext = KeywordListBox.SelectedItem;
        }
        else
        {
            EditPanel.Visibility = Visibility.Hidden;
        }
    }
}
