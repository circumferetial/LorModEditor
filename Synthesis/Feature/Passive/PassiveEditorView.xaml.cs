using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace Synthesis.Feature.Passive;

public partial class PassiveEditorView : IRegionMemberLifetime
{
    public PassiveEditorView()
    {
        InitializeComponent();

        // 监听 Loaded 事件，确保数据源绑定后立即排序
        Loaded += (_, _) => ApplyFilterAndSort();
    }

    // 保持视图状态，切回来时还在原来的位置
    public bool KeepAlive => true;

    // =============================================================
    //  纯 UI 逻辑：列表排序与过滤
    // =============================================================

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilterAndSort();
    }

    private void ApplyFilterAndSort()
    {
        if (PassiveListBox.ItemsSource == null) return;

        var view = CollectionViewSource.GetDefaultView(PassiveListBox.ItemsSource);
        if (view == null) return;

        var filterText = SearchBox.Text;

        // 1. 过滤器
        view.Filter = obj =>
        {
            if (string.IsNullOrEmpty(filterText)) return true;

            if (obj is not UnifiedPassive item) return false;
            // 搜索 ID 和 名称 (忽略大小写)
            var matchId = item.Id.Contains(filterText, StringComparison.OrdinalIgnoreCase);
            var matchName = item.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase);
            return matchId || matchName;
        };

        // 2. 排序器 (Mod 优先 + ID 排序)
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription("IsVanilla", ListSortDirection.Ascending));
        view.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));

        view.Refresh();
    }
}