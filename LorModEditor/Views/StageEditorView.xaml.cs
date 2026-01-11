using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using LorModEditor.Core;
using LorModEditor.ViewModels;

namespace LorModEditor.Views;

public partial class StageEditorView : IRegionMemberLifetime
{
    public StageEditorView()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyFilterAndSort();
    }

    private StageEditorViewModel? ViewModel => DataContext as StageEditorViewModel;
    public bool KeepAlive => true;

    // ... ApplyFilterAndSort 和 SearchBox_TextChanged 保持不变 ...
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterAndSort();

    private void ApplyFilterAndSort()
    {
        if (StageList.ItemsSource == null) return;
        var view = CollectionViewSource.GetDefaultView(StageList.ItemsSource);
        if (view != null)
        {
            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription("IsVanilla", ListSortDirection.Ascending));
            view.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));

            var filterText = SearchBox.Text;
            view.Filter = obj =>
            {
                if (string.IsNullOrEmpty(filterText)) return true;
                if (obj is UnifiedStage item)
                    return item.Id.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                           item.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase);
                return false;
            };
        }
    }

    // 修改 SelectionChanged：只负责显示 EditPanel
    private void StageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        EditPanel.Visibility = StageList.SelectedItem is UnifiedStage
            ? Visibility.Visible
            :
            // 不需要手动 LoadStoryImage 了，绑定的 ImageSelector 会自动更新
            Visibility.Hidden;
    }

    private void RemoveWave_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is UnifiedWave wave) ViewModel?.RemoveWaveCommand.Execute(wave);
    }

    // ... AddUnit, RemoveUnit, AddInvBook, RemoveInvBook 保持不变 ...
    private void AddUnit_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        var dockPanel = btn?.Parent as DockPanel;
        var comboBox = dockPanel?.Children.OfType<ComboBox>().FirstOrDefault();
        var wave = btn?.DataContext as UnifiedWave;
        var enemyId = comboBox?.SelectedValue;

        if (wave != null && enemyId is LorId id) wave.AddUnit(id);
    }

    private void RemoveUnit_Click(object sender, RoutedEventArgs e)
    {
        var btn = sender as Button;
        if (btn?.DataContext is LorId unitId && btn.Tag is UnifiedWave wave)
        {
            wave.RemoveUnit(unitId);
        }
    }

    private void AddInvBook_Click(object sender, RoutedEventArgs e) => ViewModel?.AddInvBookCommand.Execute();

    private void RemoveInvBook_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is LorId bookId) ViewModel?.RemoveInvBookCommand.Execute(bookId);
    }
}