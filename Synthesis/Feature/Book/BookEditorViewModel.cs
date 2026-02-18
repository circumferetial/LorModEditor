using System.Windows;
using Synthesis.Core;
using Synthesis.Feature.Card;
using Synthesis.Feature.Passive;

namespace Synthesis.Feature.Book;

public class BookEditorViewModel : BindableBase
{
    public BookEditorViewModel(ProjectManager manager)
    {
        Manager = manager;
        try
        {
            CreateCommand = new DelegateCommand(CreateBook);
            DeleteCommand =
                new DelegateCommand(DeleteBook, () => SelectedItem != null).ObservesProperty(() => SelectedItem);
            AddPassiveCommand =
                new DelegateCommand(AddPassive, () => SelectedItem != null && SelectedPassiveToAdd != null)
                    .ObservesProperty(() => SelectedItem).ObservesProperty(() => SelectedPassiveToAdd);
            RemovePassiveCommand = new DelegateCommand<LorId?>(RemovePassive);
            AddOnlyCardCommand =
                new DelegateCommand<UnifiedCard>(AddOnlyCard, _ => SelectedItem != null).ObservesProperty(() =>
                    SelectedItem);
            RemoveOnlyCardCommand =
                new DelegateCommand<LorId?>(RemoveOnlyCard, _ => SelectedItem != null).ObservesProperty(() =>
                    SelectedItem);
        }
        catch (Exception ex)
        {
            MessageBox.Show("ViewModel 初始化崩溃: " + ex.Message);
            throw;
        }
    }

    public ProjectManager Manager { get; }

    public UnifiedBook? SelectedItem
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                DeleteCommand.RaiseCanExecuteChanged();
                AddPassiveCommand.RaiseCanExecuteChanged();
                AddOnlyCardCommand.RaiseCanExecuteChanged();
                RemoveOnlyCardCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public UnifiedPassive? SelectedPassiveToAdd
    {
        get;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand CreateCommand { get; }

    public DelegateCommand DeleteCommand { get; }

    public DelegateCommand AddPassiveCommand { get; }

    public DelegateCommand<LorId?> RemovePassiveCommand { get; }

    public DelegateCommand<UnifiedCard> AddOnlyCardCommand { get; }

    public DelegateCommand<LorId?> RemoveOnlyCardCommand { get; }

    private void CreateBook()
    {
        try
        {
            Manager.BookRepo.Create();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void DeleteBook()
    {
        if (SelectedItem != null &&
            MessageBox.Show("确定删除书页 [" + SelectedItem.DisplayName + "]？", "提示", MessageBoxButton.YesNo) ==
            MessageBoxResult.Yes)
        {
            Manager.BookRepo.Delete(SelectedItem);
            SelectedItem = null;
        }
    }

    private void AddPassive()
    {
        if (SelectedItem != null && SelectedPassiveToAdd != null)
        {
            var globalId = SelectedPassiveToAdd.GlobalId;
            if (SelectedItem.Passives.Contains(globalId))
            {
                MessageBox.Show("该被动已存在！");
            }
            else
            {
                SelectedItem.AddPassive(globalId);
            }
        }
    }

    private void RemovePassive(LorId? pid)
    {
        if (pid.HasValue)
        {
            SelectedItem?.RemovePassive(pid.Value);
        }
    }

    private void AddOnlyCard(UnifiedCard? card)
    {
        if (SelectedItem != null && card != null)
        {
            var globalId = card.GlobalId;
            if (!SelectedItem.OnlyCards.Contains(globalId))
            {
                SelectedItem.AddOnlyCard(globalId);
            }
        }
    }

    private void RemoveOnlyCard(LorId? cid)
    {
        if (SelectedItem != null && cid.HasValue)
        {
            SelectedItem.RemoveOnlyCard(cid.Value);
        }
    }
}
