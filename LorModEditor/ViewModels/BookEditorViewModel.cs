using System.Windows;
using LorModEditor.Core;

namespace LorModEditor.ViewModels;

public class BookEditorViewModel : BindableBase
{
    // --- 选中项 ---

    // --- 临时选中项 (用于 ComboBox) ---

    public BookEditorViewModel(ProjectManager manager)
    {
        Manager = manager;

        // 基础命令
        CreateCommand = new DelegateCommand(CreateBook);
        DeleteCommand = new DelegateCommand(DeleteBook, () => SelectedItem != null)
            .ObservesProperty(() => SelectedItem);

        // 关联操作命令
        AddPassiveCommand = new DelegateCommand(AddPassive, () => SelectedItem != null && SelectedPassiveToAdd != null)
            .ObservesProperty(() => SelectedItem)
            .ObservesProperty(() => SelectedPassiveToAdd);

        // 带参数的删除命令 (删除已绑定的被动)
        RemovePassiveCommand = new DelegateCommand<LorId?>(RemovePassive);

        AddOnlyCardCommand = new DelegateCommand(AddOnlyCard, () => SelectedItem != null && SelectedCardToAdd != null)
            .ObservesProperty(() => SelectedItem)
            .ObservesProperty(() => SelectedCardToAdd);

        RemoveOnlyCardCommand = new DelegateCommand<LorId?>(RemoveOnlyCard);
    }

    public ProjectManager Manager { get; }

    public UnifiedBook? SelectedItem
    {
        get;
        set => SetProperty(ref field, value);
    }

    public UnifiedPassive? SelectedPassiveToAdd
    {
        get;
        set => SetProperty(ref field, value);
    }

    public UnifiedCard? SelectedCardToAdd
    {
        get;
        set => SetProperty(ref field, value);
    }

    // --- 命令 ---
    public DelegateCommand CreateCommand { get; }
    public DelegateCommand DeleteCommand { get; }
    public DelegateCommand AddPassiveCommand { get; }
    public DelegateCommand<LorId?> RemovePassiveCommand { get; }
    public DelegateCommand AddOnlyCardCommand { get; }
    public DelegateCommand<LorId?> RemoveOnlyCardCommand { get; }

    // --- 逻辑 ---
    private void CreateBook()
    {
        try
        {
            Manager.BookRepo.Create();
            // 选中新项逻辑可由 View 层辅助或在此实现
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteBook()
    {
        if (SelectedItem != null &&
            MessageBox.Show($"确定删除书页 [{SelectedItem.DisplayName}]？", "提示", MessageBoxButton.YesNo) ==
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
            var pid = SelectedPassiveToAdd.GlobalId;
            if (SelectedItem.Passives.Contains(pid))
            {
                MessageBox.Show("该被动已存在！");
                return;
            }
            SelectedItem.AddPassive(pid);
        }
    }

    private void RemovePassive(LorId? pid)
    {
        if (pid.HasValue) SelectedItem?.RemovePassive(pid.Value);
    }

    private void AddOnlyCard()
    {
        if (SelectedItem != null && SelectedCardToAdd != null)
        {
            var cid = SelectedCardToAdd.GlobalId;
            if (SelectedItem.OnlyCards.Contains(cid))
            {
                MessageBox.Show("该卡牌已绑定！");
                return;
            }
            SelectedItem.AddOnlyCard(cid);
        }
    }

    private void RemoveOnlyCard(LorId? cid)
    {
        if (cid.HasValue) SelectedItem?.RemoveOnlyCard(cid.Value);
    }
}