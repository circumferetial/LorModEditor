using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Synthesis.Feature.Ability;

public partial class AbilityEditorView : IRegionMemberLifetime
{
    public AbilityEditorView()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplySorting();
    }

    public bool KeepAlive => true;

    private void ApplySorting()
    {
        if (AbilityListBox.ItemsSource == null) return;

        var view = CollectionViewSource.GetDefaultView(AbilityListBox.ItemsSource);
        if (view == null) return;
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription("IsVanilla", ListSortDirection.Ascending));
        view.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
        view.Refresh();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var textBox = sender as TextBox;
        var filterText = textBox?.Text ?? "";

        var view = CollectionViewSource.GetDefaultView(AbilityListBox.ItemsSource);
        view?.Filter = obj =>
        {
            if (string.IsNullOrEmpty(filterText)) return true;

            // 【正确转换】
            if (obj is UnifiedAbility item)
            {
                // 搜 ID 或 描述
                return item.Id.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                       item.Desc.Contains(filterText, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        };
    }

    private void AbilityListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (AbilityListBox.SelectedItem != null)
        {
            EditPanel.Visibility = Visibility.Visible;
            EditPanel.DataContext = AbilityListBox.SelectedItem;
        }
        else
        {
            EditPanel.Visibility = Visibility.Hidden;
        }
    }
}