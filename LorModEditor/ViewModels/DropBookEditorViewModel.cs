using System.Windows;
using LorModEditor.Core;

namespace LorModEditor.ViewModels;

public class DropBookEditorViewModel : BindableBase
{
    public DropBookEditorViewModel(ProjectManager manager)
    {
        Manager = manager;

        // 基础命令
        CreateCommand = new DelegateCommand(() => Manager.DropBookRepo.Create());
        DeleteCommand = new DelegateCommand(Delete, () => SelectedItem != null).ObservesProperty(() => SelectedItem);

        // 掉落项命令 (Drop Items)
        AddDropCommand =
            new DelegateCommand(() => SelectedItem?.AddDropItem(), () => SelectedItem != null).ObservesProperty(() =>
                SelectedItem);
        RemoveDropCommand = new DelegateCommand<UnifiedDropItem>(item => SelectedItem?.RemoveDropItem(item));

        // 卡牌掉落表命令 (Card Drop Table)
        CreateCardDropCommand =
            new DelegateCommand(CreateCardDrop, () => SelectedItem != null && CurrentCardDrop == null)
                .ObservesProperty(() => SelectedItem)
                .ObservesProperty(() => CurrentCardDrop);// 只有当没表的时候才能创建

        AddCardDropCommand =
            new DelegateCommand(AddCardDrop, () => CurrentCardDrop != null && SelectedCardToAdd != null)
                .ObservesProperty(() => CurrentCardDrop)
                .ObservesProperty(() => SelectedCardToAdd);

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
                RefreshCardDrop();// 切换书时刷新下方的卡牌掉落表
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
            Manager.DropBookRepo.Delete(SelectedItem);
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
            var id = SelectedCardToAdd.GlobalId;
            if (!CurrentCardDrop.CardIds.Contains(id)) CurrentCardDrop.AddCard(id);
        }
    }

    private void RemoveCardDrop(LorId? id)
    {
        if (CurrentCardDrop != null && id.HasValue) CurrentCardDrop.RemoveCard(id.Value);
    }
}