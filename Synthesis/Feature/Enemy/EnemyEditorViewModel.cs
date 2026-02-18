using System.Windows;
using Synthesis.Core;
using Synthesis.Feature.Card;

namespace Synthesis.Feature.Enemy;

public class EnemyEditorViewModel : BindableBase
{
    public EnemyEditorViewModel(ProjectManager manager)
    {
        Manager = manager;
        CreateCommand = new DelegateCommand(delegate { Manager.EnemyRepo.Create(); });
        DeleteCommand = new DelegateCommand(Delete, () => SelectedItem != null).ObservesProperty(() => SelectedItem);
        AddDropCommand = new DelegateCommand(AddDrop, () => SelectedItem != null).ObservesProperty(() => SelectedItem);
        RemoveDropCommand = new DelegateCommand<UnifiedEnemyDrop>(RemoveDrop);
        AddCardToDeckCommand = new DelegateCommand<UnifiedCard>(AddCardToDeck);
        RemoveCardFromDeckCommand = new DelegateCommand<object>(RemoveCardFromDeck);
    }

    public ProjectManager Manager { get; }

    public UnifiedEnemy? SelectedItem
    {
        get;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand CreateCommand { get; }

    public DelegateCommand DeleteCommand { get; }

    public DelegateCommand AddDropCommand { get; }

    public DelegateCommand<UnifiedEnemyDrop> RemoveDropCommand { get; }

    public DelegateCommand<UnifiedCard> AddCardToDeckCommand { get; }

    public DelegateCommand<object> RemoveCardFromDeckCommand { get; }

    private void Delete()
    {
        if (SelectedItem != null &&
            MessageBox.Show("确定删除敌人 [" + SelectedItem.DisplayName + "]？", "提示", MessageBoxButton.YesNo) ==
            MessageBoxResult.Yes)
        {
            Manager.EnemyRepo.Delete(SelectedItem);
            SelectedItem = null;
        }
    }

    private void AddDrop()
    {
        if (SelectedItem != null)
        {
            var lorId = Manager.DropBookRepo.Items.FirstOrDefault()?.GlobalId;
            if (lorId.HasValue)
            {
                SelectedItem.AddDrop(lorId.Value);
            }
        }
    }

    private void RemoveDrop(UnifiedEnemyDrop drop)
    {
        SelectedItem?.RemoveDrop(drop);
    }

    private void AddCardToDeck(UnifiedCard? card)
    {
        if (SelectedItem != null && card != null)
        {
            SelectedItem.AddCardToDeck(card.GlobalId);
        }
    }

    private void RemoveCardFromDeck(object o)
    {
        if (SelectedItem != null && o is LorId cid)
        {
            SelectedItem.RemoveCardFromDeck(cid);
        }
    }
}
