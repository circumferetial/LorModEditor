using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Synthesis.Core.Tools;

namespace Synthesis.Feature.Book;

public partial class BookEditorView : UserControl, IRegionMemberLifetime
{
    private readonly DispatcherTimer _searchTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(400)
    };

    public BookEditorView()
    {
        try
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
        catch (Exception ex)
        {
            MessageBox.Show(
                "界面加载失败！\n\n错误信息: " + ex.Message + "\n\n内部错误: " + ex.InnerException?.Message,
                "XAML Error",
                MessageBoxButton.OK,
                MessageBoxImage.Hand);
        }
    }

    public bool KeepAlive => true;

    private void ApplySorting()
    {
        if (BookList.ItemsSource != null)
        {
            ViewSortHelper.ApplyModFirstNaturalSort<UnifiedBook>(BookList.ItemsSource, x => x.Id);
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

            return obj is UnifiedBook book &&
                   (book.Id.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                    book.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase));
        };
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchTimer.Stop();
        _searchTimer.Start();
    }

    private void IgnoreMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not UIElement sourceElement)
        {
            return;
        }

        e.Handled = true;
        var bubbleEvent = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
        {
            RoutedEvent = MouseWheelEvent,
            Source = sender
        };
        (VisualTreeHelper.GetParent(sourceElement) as UIElement)?.RaiseEvent(bubbleEvent);
    }

    private void GenDialog_Click(object sender, RoutedEventArgs e)
    {
        if (EditPanel.DataContext is not UnifiedBook book ||
            DataContext is not BookEditorViewModel viewModel ||
            MessageBox.Show($"为 [{book.Id}] 生成对话？", "确认", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            viewModel.Manager.DialogRepo.CreateTemplate(book.Id);
            MessageBox.Show("完成");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
}
