using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Synthesis.Feature.Keyword;

public partial class KeywordEditorView : IRegionMemberLifetime
{
    public KeywordEditorView()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplySorting();
    }

    public bool KeepAlive => true;

    private void ApplySorting()
    {
        if (KeywordListBox.ItemsSource == null) return;

        var view = CollectionViewSource.GetDefaultView(KeywordListBox.ItemsSource);
        if (view != null)
        {
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("IsVanilla", ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
            view.Refresh();
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = sender as TextBox;
        var filterText = textBox?.Text ?? "";

        var view = CollectionViewSource.GetDefaultView(KeywordListBox.ItemsSource);
        if (view != null)
        {
            view.Filter = obj =>
            {
                if (string.IsNullOrEmpty(filterText)) return true;

                // 【核心修正】这里是 UnifiedKeyword
                if (obj is UnifiedKeyword item)
                {
                    // 搜索 ID, Name 和 Desc
                    return item.Id.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                           item.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                           item.Desc.Contains(filterText, StringComparison.OrdinalIgnoreCase);
                }
                return false;
            };
        }
    }

    private void KeywordListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (KeywordListBox.SelectedItem != null)
        {
            EditPanel.Visibility = Visibility.Visible;
            EditPanel.DataContext = KeywordListBox.SelectedItem;
        }
        else
        {
            EditPanel.Visibility = Visibility.Hidden;
        }
    }
}