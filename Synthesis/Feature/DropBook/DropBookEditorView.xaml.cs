using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Synthesis.Core.Tools;

namespace Synthesis.Feature.DropBook;

public partial class DropBookEditorView : UserControl, IRegionMemberLifetime
{
    private readonly DispatcherTimer _searchTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(400)
    };

    public DropBookEditorView()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ApplySorting();
            ApplyFilter();
        };
        _searchTimer.Tick += (_, _) =>
        {
            _searchTimer.Stop();
            ApplyFilter();
        };
    }

    public bool KeepAlive => true;

    private void ApplySorting()
    {
        if (BookList.ItemsSource != null)
        {
            ViewSortHelper.ApplyModFirstNaturalSort<UnifiedDropBook>(BookList.ItemsSource, x => x.Id);
        }
    }

    private void ApplyFilter()
    {
        if (BookList.ItemsSource == null)
        {
            return;
        }

        var view = CollectionViewSource.GetDefaultView(BookList.ItemsSource);
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

            return obj is UnifiedDropBook book &&
                   (book.Id.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                    book.LocalizedName.Contains(filterText, StringComparison.OrdinalIgnoreCase));
        };
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private void BookList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        EditPanel.Visibility = BookList.SelectedItem == null ? Visibility.Hidden : Visibility.Visible;
    }
}
