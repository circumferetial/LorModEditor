using System.Windows;
using LorModEditor.Core;

namespace LorModEditor.ViewModels;

public class EnemyEditorViewModel : BindableBase
{
    public EnemyEditorViewModel(ProjectManager manager)
    {
        Manager = manager;

        // 基础 CRUD
        CreateCommand = new DelegateCommand(() => Manager.EnemyRepo.Create());
        DeleteCommand = new DelegateCommand(Delete, () => SelectedItem != null)
            .ObservesProperty(() => SelectedItem);

        // 掉落操作
        AddDropCommand = new DelegateCommand(AddDrop, () => SelectedItem != null).ObservesProperty(() => SelectedItem);
        RemoveDropCommand = new DelegateCommand<UnifiedEnemyDrop>(RemoveDrop);

        // 卡组操作
        // 添加卡牌 (需要传入 UnifiedCard)
        AddCardToDeckCommand = new DelegateCommand<UnifiedCard>(AddCardToDeck);
        // 移除卡牌 (需要传入 LorId)
        RemoveCardFromDeckCommand = new DelegateCommand<object>(RemoveCardFromDeck);
    }

    public ProjectManager Manager { get; }

    public UnifiedEnemy? SelectedItem
    {
        get;
        set => SetProperty(ref field, value);
    }

    // 命令
    public DelegateCommand CreateCommand { get; }
    public DelegateCommand DeleteCommand { get; }
    public DelegateCommand AddDropCommand { get; }
    public DelegateCommand<UnifiedEnemyDrop> RemoveDropCommand { get; }
    public DelegateCommand<UnifiedCard> AddCardToDeckCommand { get; }
    public DelegateCommand<object> RemoveCardFromDeckCommand { get; }

    // 逻辑实现
    private void Delete()
    {
        if (SelectedItem == null ||
            MessageBox.Show($"确定删除敌人 [{SelectedItem.DisplayName}]？", "提示", MessageBoxButton.YesNo) !=
            MessageBoxResult.Yes) return;
        Manager.EnemyRepo.Delete(SelectedItem);
        SelectedItem = null;
    }

    private void AddDrop()
    {
        if (SelectedItem == null) return;
        // 默认选第一本书，或者给个原版 ID
        var defaultBook = Manager.DropBookRepo.Items.FirstOrDefault()?.GlobalId;
        if (defaultBook != null) SelectedItem.AddDrop(defaultBook.Value);
    }

    private void RemoveDrop(UnifiedEnemyDrop drop) => SelectedItem?.RemoveDrop(drop);

    private void AddCardToDeck(UnifiedCard? card)
    {
        if (SelectedItem != null && card != null)
        {
            SelectedItem.AddCardToDeck(card.GlobalId);
        }
    }

    private void RemoveCardFromDeck(object o)
    {
        MessageBox.Show("hh");
        if (SelectedItem != null && o is LorId cardId)
        {
            SelectedItem.RemoveCardFromDeck(cardId);
        }
    }
}