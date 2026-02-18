using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Synthesis.Core.Tools;

namespace Synthesis.Feature.Card;

public partial class CardEditorView : UserControl, IRegionMemberLifetime
{
    private readonly DispatcherTimer _searchDebounceTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(300)
    };

    private bool _isFilterInitialized;
    private bool _isSortApplied;
    private string _lastFilterText = string.Empty;

    public CardEditorView()
    {
        InitializeComponent();
        _searchDebounceTimer.Tick += (_, _) =>
        {
            _searchDebounceTimer.Stop();
            ApplyFilterAndSort();
        };
        Loaded += (_, _) => ApplyFilterAndSort();
    }

    public bool KeepAlive => true;

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    private void ApplyFilterAndSort()
    {
        if (CardListBox.ItemsSource == null ||
            CollectionViewSource.GetDefaultView(CardListBox.ItemsSource) is not ListCollectionView view)
        {
            return;
        }

        var needsRefresh = false;
        if (!_isSortApplied)
        {
            view.CustomSort = new ModFirstNaturalComparer<UnifiedCard>(x => x.Id);
            _isSortApplied = true;
            needsRefresh = true;
        }

        if (!_isFilterInitialized)
        {
            view.Filter = FilterCard;
            _isFilterInitialized = true;
            needsRefresh = true;
        }

        var filter = SearchBox.Text.Trim();
        if (filter != _lastFilterText)
        {
            _lastFilterText = filter;
            needsRefresh = true;
        }

        if (needsRefresh)
        {
            view.Refresh();
        }
    }

    private bool FilterCard(object obj)
    {
        if (obj is not UnifiedCard card)
        {
            return false;
        }

        if (string.IsNullOrEmpty(_lastFilterText))
        {
            return true;
        }

        return card.Id.Contains(_lastFilterText, StringComparison.OrdinalIgnoreCase) ||
               card.Name.Contains(_lastFilterText, StringComparison.OrdinalIgnoreCase);
    }

    private void CardListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        EditPanel.Visibility = CardListBox.SelectedItem == null ? Visibility.Hidden : Visibility.Visible;
    }
}
