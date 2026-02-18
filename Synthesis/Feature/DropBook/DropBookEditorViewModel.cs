using System.Windows;
using Synthesis.Core;
using Synthesis.Feature.Card;
using Synthesis.Feature.DropBook.CardDrop;

namespace Synthesis.Feature.DropBook;

public class DropBookEditorViewModel : BindableBase
{
    public DropBookEditorViewModel(ProjectManager manager)
    {
        Manager = manager;
        CreateCommand = new DelegateCommand(delegate { Manager.DropBookRepo.Create(); });
        DeleteCommand = new DelegateCommand(Delete, () => SelectedItem != null).ObservesProperty(() => SelectedItem);
        AddDropCommand =
            new DelegateCommand(delegate { SelectedItem?.AddDropItem(); }, () => SelectedItem != null)
                .ObservesProperty(() => SelectedItem);
        RemoveDropCommand = new DelegateCommand<UnifiedDropItem>(delegate(UnifiedDropItem item)
        {
            SelectedItem?.RemoveDropItem(item);
        });
        CreateCardDropCommand =
            new DelegateCommand(CreateCardDrop, () => SelectedItem != null && CurrentCardDrop == null)
                .ObservesProperty(() => SelectedItem).ObservesProperty(() => CurrentCardDrop);
        AddCardDropCommand =
            new DelegateCommand(AddCardDrop, () => CurrentCardDrop != null && SelectedCardToAdd != null)
                .ObservesProperty(() => CurrentCardDrop).ObservesProperty(() => SelectedCardToAdd);
        RemoveCardDropCommand = new DelegateCommand<LorId?>(RemoveCardDrop);
    }

    public ProjectManager Manager { get; }

    public UnifiedDropBook? SelectedItem
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                RefreshCardDrop();
            }
        }
    }

    public UnifiedCardDrop? CurrentCardDrop
    {
        get;
        set => SetProperty(ref field, value);
    }

    public UnifiedCard? SelectedCardToAdd
    {
        get;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand CreateCommand { get; }

    public DelegateCommand DeleteCommand { get; }

    public DelegateCommand AddDropCommand { get; }

    public DelegateCommand<UnifiedDropItem> RemoveDropCommand { get; }

    public DelegateCommand CreateCardDropCommand { get; }

    public DelegateCommand AddCardDropCommand { get; }

    public DelegateCommand<LorId?> RemoveCardDropCommand { get; }

    private void Delete()
    {
        if (SelectedItem != null && MessageBox.Show("确认删除?", "提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            Manager.DropBookRepo.Delete(SelectedItem);
        }
    }

    private void RefreshCardDrop()
    {
        CurrentCardDrop = SelectedItem == null ? null : Manager.CardDropRepo.GetByBookId(SelectedItem.Id);
    }

    private void CreateCardDrop()
    {
        if (SelectedItem != null)
        {
            try
            {
                Manager.CardDropRepo.Create(SelectedItem.Id);
                RefreshCardDrop();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }

    private void AddCardDrop()
    {
        if (CurrentCardDrop != null && SelectedCardToAdd != null)
        {
            var globalId = SelectedCardToAdd.GlobalId;
            if (!CurrentCardDrop.CardIds.Contains(globalId))
            {
                CurrentCardDrop.AddCard(globalId);
            }
        }
    }

    private void RemoveCardDrop(LorId? id)
    {
        if (CurrentCardDrop != null && id.HasValue)
        {
            CurrentCardDrop.RemoveCard(id.Value);
        }
    }
}
