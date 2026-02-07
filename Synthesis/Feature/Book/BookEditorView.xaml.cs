using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Synthesis.Feature.Book;

public partial class BookEditorView : UserControl, IRegionMemberLifetime
{
    public BookEditorView()
    {
        // 【关键】增加 Try-Catch 来捕获 XAML 加载错误
        try
        {
            InitializeComponent();
            Loaded += (_, _) => ApplyFilterAndSort();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"界面加载失败！\n\n错误信息: {ex.Message}\n\n内部错误: {ex.InnerException?.Message}",
                "XAML Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public bool KeepAlive => true;
// 在 BookEditorView.xaml.cs 内部添加这个方法

    private void IgnoreMouseWheel(object sender, MouseWheelEventArgs e)
    {
        // 1. 标记事件未被处理，让它继续向上冒泡
        // (注意：WPF ListBox 默认会吃掉这个事件，所以我们需要手动捕获 Tunneling 或者 Preview 事件)

        if (sender is not UIElement element) return;

        // 2. 这里的逻辑是：既然在这个控件上滚动了，但我不想让它内部滚，
        //    所以我手动找到父级 ScrollViewer (或者让事件冒泡)，
        //    但在 Preview 阶段，最简单的做法是：

        // 捕获事件，标记为已处理，然后手动触发父级的滚动
        e.Handled = true;

        var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = MouseWheelEvent,
            Source = sender
        };

        var parent = VisualTreeHelper.GetParent(element) as UIElement;
        parent?.RaiseEvent(eventArg);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterAndSort();

    private void ApplyFilterAndSort()
    {
        if (BookList == null || BookList.ItemsSource == null) return;

        var view = CollectionViewSource.GetDefaultView(BookList.ItemsSource);
        if (view == null) return;

        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription("IsVanilla", ListSortDirection.Ascending));
        view.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));

        var filterText = SearchBox?.Text ?? "";
        view.Filter = obj =>
        {
            if (string.IsNullOrEmpty(filterText)) return true;
            if (obj is UnifiedBook item)
                return item.Id.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                       item.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase);
            return false;
        };
    }


    private void GenDialog_Click(object sender, RoutedEventArgs e)
    {
        if (EditPanel?.DataContext is UnifiedBook book && DataContext is BookEditorViewModel vm)
        {
            if (MessageBox.Show($"为 [{book.Id}] 生成对话？", "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                try
                {
                    vm.Manager.DialogRepo.CreateTemplate(book.Id);
                    MessageBox.Show("完成");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
    }
}