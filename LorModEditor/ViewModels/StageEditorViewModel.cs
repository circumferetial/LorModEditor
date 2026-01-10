using System.Windows;
using LorModEditor.Core;

namespace LorModEditor.ViewModels;

public class StageEditorViewModel : BindableBase
{
    // 界面上选中的“邀请函书籍”

    // 界面上选中的“要添加的敌人” (所有波次共用一个选择器，简化UI)

    public StageEditorViewModel(ProjectManager manager)
    {
        Manager = manager;

        CreateCommand = new DelegateCommand(() => Manager.StageRepo.Create());
        DeleteCommand = new DelegateCommand(Delete, () => SelectedItem != null)
            .ObservesProperty(() => SelectedItem);

        // 波次操作
        AddWaveCommand = new DelegateCommand(() => SelectedItem?.AddWave(), () => SelectedItem != null)
            .ObservesProperty(() => SelectedItem);

        // 删除波次 (参数是 UnifiedWave)
        RemoveWaveCommand = new DelegateCommand<UnifiedWave>(w => SelectedItem?.RemoveWave(w));

        // 邀请函操作
        AddInvBookCommand = new DelegateCommand(AddInvBook);
        RemoveInvBookCommand = new DelegateCommand<LorId?>(RemoveInvBook);

        // 敌人操作 (最复杂的部分)
        // 添加敌人：需要知道加到哪个 Wave，以及加哪个 Enemy
        // 我们在 View 层通过 CommandParameter 传递 UnifiedWave
        AddUnitCommand = new DelegateCommand<UnifiedWave>(AddUnit);
    }

    public ProjectManager Manager { get; }

    public UnifiedStage? SelectedItem
    {
        get;
        set => SetProperty(ref field, value);
    }

    public UnifiedDropBook? SelectedDropBook
    {
        get;
        set => SetProperty(ref field, value);
    }

    public UnifiedEnemy? SelectedEnemyToAdd
    {
        get;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand CreateCommand { get; }
    public DelegateCommand DeleteCommand { get; }
    public DelegateCommand AddWaveCommand { get; }
    public DelegateCommand<UnifiedWave> RemoveWaveCommand { get; }
    public DelegateCommand AddInvBookCommand { get; }
    public DelegateCommand<LorId?> RemoveInvBookCommand { get; }
    public DelegateCommand<UnifiedWave> AddUnitCommand { get; }


    private void Delete()
    {
        if (SelectedItem != null && MessageBox.Show("确定删除?", "提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            Manager.StageRepo.Delete(SelectedItem);
            SelectedItem = null;
        }
    }

    private void AddInvBook()
    {
        if (SelectedItem != null && SelectedDropBook != null)
        {
            var bid = SelectedDropBook.GlobalId;
            if (!SelectedItem.InvitationBooks.Contains(bid)) SelectedItem.AddInvitationBook(bid);
        }
    }

    private void RemoveInvBook(LorId? bid)
    {
        if (bid.HasValue) SelectedItem?.RemoveInvitationBook(bid.Value);
    }

    // 添加敌人到指定波次
    private void AddUnit(UnifiedWave? wave)
    {
        if (wave != null && SelectedEnemyToAdd != null)
        {
            wave.AddUnit(SelectedEnemyToAdd.GlobalId);
        }
    }
}