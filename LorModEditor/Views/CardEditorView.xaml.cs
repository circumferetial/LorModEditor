using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LorModEditor.Views;

public partial class CardEditorView : IRegionMemberLifetime
{
    private readonly CardSorter _sorter = new();

    public CardEditorView()
    {
        InitializeComponent();

        // 监听 Loaded 事件，确保界面显示出来后立即应用排序
        Loaded += (_, _) => ApplyFilterAndSort();
    }

    public bool KeepAlive => true;

    // =============================================================
    //  列表排序与过滤 (View 层负责)
    // =============================================================
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilterAndSort();
    }

    private void ApplyFilterAndSort()
    {
        if (CardListBox.ItemsSource == null) return;

        // 获取视图
        if (CollectionViewSource.GetDefaultView(CardListBox.ItemsSource) is not ListCollectionView view) return;

        var filterText = SearchBox.Text;

        // 1. 过滤器
        view.Filter = obj =>
        {
            if (string.IsNullOrEmpty(filterText)) return true;
            if (obj is UnifiedCard card)
            {
                var matchId = card.Id.Contains(filterText, StringComparison.OrdinalIgnoreCase);
                var matchName = card.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase);
                return matchId || matchName;
            }
            return false;
        };

        // 2. 排序器 (Mod 优先 + ID 排序)
        view.CustomSort = _sorter;

        // 3. 刷新
        view.Refresh();
    }

    // =============================================================
    //  SelectionChanged
    //  (现在只负责显示/隐藏面板，不需要手动加载图片了)
    // =============================================================
    private void CardListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CardListBox.SelectedItem != null)
        {
            EditPanel.Visibility = Visibility.Visible;
            // 注意：EditPanel.DataContext 已经在 XAML 里绑定了，不需要手动设
        }
        else
        {
            EditPanel.Visibility = Visibility.Hidden;
        }
    }
}

// 排序器类
internal class CardSorter : IComparer
{
    public int Compare(object? x, object? y)
    {
        if (x is not UnifiedCard cardX || y is not UnifiedCard cardY) return 0;

        // 1. Mod 内容置顶 (IsVanilla: False < True)
        if (cardX.IsVanilla != cardY.IsVanilla)
        {
            return cardX.IsVanilla.CompareTo(cardY.IsVanilla);
        }

        // 2. ID 数值排序
        var xIsInt = int.TryParse(cardX.Id, out var idX);
        var yIsInt = int.TryParse(cardY.Id, out var idY);

        if (xIsInt && yIsInt)
        {
            return idX.CompareTo(idY);
        }

        // 3. 兜底字符串排序
        return string.Compare(cardX.Id, cardY.Id, StringComparison.OrdinalIgnoreCase);
    }
}