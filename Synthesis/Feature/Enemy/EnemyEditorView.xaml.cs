using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Synthesis.Feature.Enemy;

public partial class EnemyEditorView : IRegionMemberLifetime
{
    public EnemyEditorView()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyFilterAndSort();
    }

    public bool KeepAlive => true;

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilterAndSort();
    }

    private void ApplyFilterAndSort()
    {
        if (EnemyList.ItemsSource == null) return;
        var view = CollectionViewSource.GetDefaultView(EnemyList.ItemsSource);
        if (view != null)
        {
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("IsVanilla", ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));

            var filterText = SearchBox.Text;
            view.Filter = obj =>
            {
                if (string.IsNullOrEmpty(filterText)) return true;
                if (obj is UnifiedEnemy item)
                    return item.Id.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                           item.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase);
                return false;
            };
        }
    }

    // 纯 UI 行为：滚动到顶部
    private void EnemyList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EditPanel.Visibility == Visibility.Visible)
            ScrollViewer.ScrollToTop();
    }

    // 清除书页的逻辑比较简单，可以留在这里，或者在 ViewModel 里加 ClearBookCommand
    private void ClearBook_Click(object sender, RoutedEventArgs e)
    {
        if (EditPanel.DataContext is UnifiedEnemy enemy) enemy.BookId = default;
    }
}