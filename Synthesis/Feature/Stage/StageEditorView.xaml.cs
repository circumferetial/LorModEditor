using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Synthesis.Core;
using Synthesis.Feature.DropBook;

namespace Synthesis.Feature.Stage;

public partial class StageEditorView : IRegionMemberLifetime
{
    public StageEditorView()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplyFilterAndSort();

        // 邀请函书籍搜索
        InvBookCombo.AddHandler(TextBoxBase.TextChangedEvent,
            new TextChangedEventHandler(Combo_TextChanged));
    }

    private StageEditorViewModel? ViewModel => DataContext as StageEditorViewModel;
    public bool KeepAlive => true;

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilterAndSort();

    private void ApplyFilterAndSort()
    {
        if (StageList.ItemsSource == null) return;
        var view = CollectionViewSource.GetDefaultView(StageList.ItemsSource);
        if (view == null) return;
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

    private void StageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        EditPanel.Visibility = StageList.SelectedItem != null ? Visibility.Visible : Visibility.Hidden;
    }

    private void RemoveWave_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is UnifiedWave wave)
            ViewModel?.RemoveWaveCommand.Execute(wave);
    }

    // --- 敌人操作 ---
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
        if (btn?.DataContext is not LorId unitId) return;
        // 【核心修复】不依赖 Tag，直接在视觉树里往上找
        // 目标：找到包裹这个按钮的 DataTemplate 对应的数据对象 (UnifiedWave)

        var wave = FindParentDataContext<UnifiedWave>(btn);

        wave?.RemoveUnit(unitId);
    }

// 通用的查找辅助方法
    private static T? FindParentDataContext<T>(DependencyObject child) where T : class
    {
        var parent = VisualTreeHelper.GetParent(child);
        while (parent != null)
        {
            if (parent is FrameworkElement { DataContext: T target })
            {
                return target;
            }
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }

    // --- 邀请函操作 ---
    private void AddInvBook_Click(object sender, RoutedEventArgs e) => ViewModel?.AddInvBookCommand.Execute();

    private void RemoveInvBook_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is LorId bookId)
            ViewModel?.RemoveInvBookCommand.Execute(bookId);
    }

    // --- 下拉框过滤 ---
    private static void Combo_TextChanged(object sender, TextChangedEventArgs e)
    {
        var comboBox = sender as ComboBox;
        if (e.OriginalSource is not TextBox textBox || comboBox == null) return;
        var filterText = textBox.Text;
        var view = CollectionViewSource.GetDefaultView(comboBox.ItemsSource);
        if (view == null) return;
        view.Filter = obj =>
        {
            if (string.IsNullOrEmpty(filterText)) return true;
            // 对 UnifiedDropBook 过滤
            if (obj is UnifiedDropBook b) return b.DisplayName.Contains(filterText, StringComparison.OrdinalIgnoreCase);
            return false;
        };
        if (comboBox is { IsKeyboardFocusWithin: true, IsDropDownOpen: false }) comboBox.IsDropDownOpen = true;
    }
}