using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using LorModEditor.ViewModels;

namespace LorModEditor.Views;

public partial class BookEditorView : IRegionMemberLifetime
{
    public BookEditorView()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyFilterAndSort();

        // 下拉框过滤监听
        PassiveSelector.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(Combo_TextChanged));
        OnlyCardSelector.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(Combo_TextChanged));
    }

    public bool KeepAlive => true;

    // 依然保留 View 层的排序和过滤逻辑
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

            var filterText = SearchBox.Text;
            view.Filter = obj =>
            {
                if (string.IsNullOrEmpty(filterText)) return true;
                if (obj is UnifiedBook item)
                    return item.Id.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                           item.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase);
                return false;
            };
        }
    }

    // 列表选中后滚动到顶部 (可选)
    private void BookList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // MVVM 已通过 SelectedItem 绑定，这里只需处理 UI 行为
        if (EditPanel.Visibility == Visibility.Visible)
        {
            // (Optional) ScrollViewer.ScrollToTop();
        }
    }

    // ComboBox 过滤逻辑 (UI行为)
    private static void Combo_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (e.OriginalSource is not TextBox textBox || sender is not ComboBox comboBox) return;

        var filterText = textBox.Text;
        var view = CollectionViewSource.GetDefaultView(comboBox.ItemsSource);
        if (view == null) return;
        view.Filter = obj =>
        {
            if (string.IsNullOrEmpty(filterText)) return true;
            return obj switch
            {
                // 通用匹配 DisplayName (假设对象都有这个属性)
                // 使用 dynamic 或者反射，或者已知类型
                UnifiedPassive p => p.DisplayName.Contains(filterText, StringComparison.OrdinalIgnoreCase),
                UnifiedCard c => c.DisplayName.Contains(filterText, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        };

        if (comboBox is { IsKeyboardFocusWithin: true, IsDropDownOpen: false }) comboBox.IsDropDownOpen = true;
    }

    // 生成对话 (这是个不涉及 Model 数据流转的纯工具功能，保留在 View 层调用逻辑层接口)
    private void GenDialog_Click(object sender, RoutedEventArgs e)
    {
        var book = EditPanel.DataContext as UnifiedBook;
        var manager = (DataContext as BookEditorViewModel)?.Manager;// 获取 Manager

        if (book == null || manager == null) return;
        if (MessageBox.Show($"将为核心书页 [{book.Id}] 生成对话模板。\n是否继续？", "确认", MessageBoxButton.YesNo) !=
            MessageBoxResult.Yes) return;
        try
        {
            manager.DialogRepo.CreateTemplate(book.Id);
            MessageBox.Show("生成成功！");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
}