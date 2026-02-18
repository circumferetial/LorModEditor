using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using Synthesis.Core.Tools;

namespace Synthesis.Feature.Enemy;

public partial class EnemyEditorView : UserControl, IRegionMemberLifetime
{
    private readonly DispatcherTimer _searchTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(400)
    };

    public EnemyEditorView()
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
        if (EnemyList.ItemsSource != null)
        {
            ViewSortHelper.ApplyModFirstNaturalSort<UnifiedEnemy>(EnemyList.ItemsSource, x => x.Id);
        }
    }

    private void ApplyFilter()
    {
        if (EnemyList.ItemsSource == null)
        {
            return;
        }

        var view = CollectionViewSource.GetDefaultView(EnemyList.ItemsSource);
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

            return obj is UnifiedEnemy enemy &&
                   (enemy.Id.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                    enemy.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase));
        };
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private void EnemyList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EditPanel.Visibility == Visibility.Visible)
        {
            ScrollViewer.ScrollToTop();
        }
    }

    private void ClearBook_Click(object sender, RoutedEventArgs e)
    {
        if (EditPanel.DataContext is UnifiedEnemy enemy)
        {
            enemy.BookId = default;
        }
    }
}
