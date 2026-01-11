using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LorModEditor.Views;

public partial class DropBookEditorView : IRegionMemberLifetime
{
    public DropBookEditorView()
    {
        InitializeComponent();

        // 列表排序 (Loaded 时触发)
        Loaded += (s, e) => ApplyFilterAndSort();
    }

    public bool KeepAlive => true;

    // =============================================================
    //  列表排序与过滤 (View 层负责)
    // =============================================================
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterAndSort();

    private void ApplyFilterAndSort()
    {
        if (BookList.ItemsSource == null) return;
        var view = CollectionViewSource.GetDefaultView(BookList.ItemsSource);
        if (view != null)
        {
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("IsVanilla", ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));

            var filterText = SearchBox.Text ?? "";
            view.Filter = o =>
            {
                if (string.IsNullOrEmpty(filterText)) return true;
                if (o is UnifiedDropBook b)
                    return b.Id.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                           b.LocalizedName.Contains(filterText, StringComparison.OrdinalIgnoreCase);
                return false;
            };
        }
    }

    // =============================================================
    //  SelectionChanged
    //  (现在只负责显示/隐藏面板，不需要手动加载图片了)
    // =============================================================
    private void BookList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (BookList.SelectedItem != null)
        {
            EditPanel.Visibility = Visibility.Visible;
            // 注意：DataContext 已经通过 XAML 的 SelectedItem 绑定了，这里不需要手动赋值
        }
        else
        {
            EditPanel.Visibility = Visibility.Hidden;
        }
    }

    // =============================================================
    //  【已删除】旧的图片加载逻辑
    //  LoadIcon, IconCombo_SelectionChanged, IconCombo_TextChanged
    //  统统删掉！因为 ImageSelector 控件已经接管了这一切。
    // =============================================================
}