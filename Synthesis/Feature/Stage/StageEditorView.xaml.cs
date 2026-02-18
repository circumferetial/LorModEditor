using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using Synthesis.Core;
using Synthesis.Core.Tools;
using Synthesis.Feature.DropBook;

namespace Synthesis.Feature.Stage;

public partial class StageEditorView : UserControl, IRegionMemberLifetime
{
    private readonly DispatcherTimer _invBookSearchTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(400)
    };

    private readonly DispatcherTimer _mainSearchTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(400)
    };

    public StageEditorView()
    {
        InitializeComponent();
        Loaded += (_, _) => ApplySorting();
        _mainSearchTimer.Tick += (_, _) =>
        {
            _mainSearchTimer.Stop();
            ApplyMainFilter();
        };
        _invBookSearchTimer.Tick += (_, _) =>
        {
            _invBookSearchTimer.Stop();
            ApplyInvBookFilter();
        };
        InvBookCombo.AddHandler(TextBoxBase.TextChangedEvent, new TextChangedEventHandler(InvBookCombo_TextChanged));
    }

    private StageEditorViewModel? ViewModel => DataContext as StageEditorViewModel;

    public bool KeepAlive => true;

    private void ApplySorting()
    {
        if (StageList.ItemsSource != null)
        {
            ViewSortHelper.ApplyModFirstNaturalSort<UnifiedStage>(StageList.ItemsSource, x => x.Id);
        }
    }

    private void ApplyMainFilter()
    {
        if (StageList.ItemsSource == null)
        {
            return;
        }

        var view = CollectionViewSource.GetDefaultView(StageList.ItemsSource);
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

            return obj is UnifiedStage stage &&
                   (stage.Id.Contains(filterText, StringComparison.OrdinalIgnoreCase) ||
                    stage.Name.Contains(filterText, StringComparison.OrdinalIgnoreCase));
        };
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _mainSearchTimer.Stop();
        _mainSearchTimer.Start();
    }

    private void InvBookCombo_TextChanged(object sender, TextChangedEventArgs e)
    {
        _invBookSearchTimer.Stop();
        _invBookSearchTimer.Start();
    }

    private void ApplyInvBookFilter()
    {
        var filterText = InvBookCombo.Text;
        var view = CollectionViewSource.GetDefaultView(InvBookCombo.ItemsSource);
        if (view == null)
        {
            return;
        }

        view.Filter = obj =>
        {
            if (string.IsNullOrEmpty(filterText))
            {
                return true;
            }

            return obj is UnifiedDropBook book &&
                   book.DisplayName.Contains(filterText, StringComparison.OrdinalIgnoreCase);
        };

        if (InvBookCombo.IsKeyboardFocusWithin && !InvBookCombo.IsDropDownOpen)
        {
            InvBookCombo.IsDropDownOpen = true;
        }
    }

    private void StageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        EditPanel.Visibility = StageList.SelectedItem == null ? Visibility.Hidden : Visibility.Visible;
    }

    private void RemoveWave_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is UnifiedWave wave)
        {
            ViewModel?.RemoveWaveCommand.Execute(wave);
        }
    }

    private void AddUnit_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var combo = (button?.Parent as DockPanel)?.Children.OfType<ComboBox>().FirstOrDefault();
        var wave = button?.DataContext as UnifiedWave;
        if (wave != null && combo?.SelectedValue is LorId unitId)
        {
            wave.AddUnit(unitId);
        }
    }

    private void RemoveUnit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: LorId unitId } button)
        {
            FindParentDataContext<UnifiedWave>(button)?.RemoveUnit(unitId);
        }
    }

    private static T? FindParentDataContext<T>(DependencyObject child) where T : class
    {
        for (var parent = VisualTreeHelper.GetParent(child);
             parent != null;
             parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is FrameworkElement { DataContext: T context })
            {
                return context;
            }
        }

        return null;
    }

    private void AddInvBook_Click(object sender, RoutedEventArgs e)
    {
        ViewModel?.AddInvBookCommand.Execute();
    }

    private void RemoveInvBook_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.DataContext is LorId id)
        {
            ViewModel?.RemoveInvBookCommand.Execute(id);
        }
    }
}
