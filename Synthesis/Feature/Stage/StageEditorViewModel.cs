using System.Windows;
using Synthesis.Core;
using Synthesis.Feature.DropBook;

namespace Synthesis.Feature.Stage;

public class StageEditorViewModel : BindableBase
{
    // --- 邀请函选择 ---

    public StageEditorViewModel(ProjectManager manager)
    {
        Manager = manager;

        CreateCommand = new DelegateCommand(() => Manager.StageRepo.Create());
        DeleteCommand = new DelegateCommand(Delete, () => SelectedItem != null).ObservesProperty(() => SelectedItem);

        AddWaveCommand = new DelegateCommand(AddWave, () => SelectedItem != null).ObservesProperty(() => SelectedItem);
        RemoveWaveCommand = new DelegateCommand<UnifiedWave>(RemoveWave);

        AddInvBookCommand =
            new DelegateCommand(AddInvBook, () => SelectedItem != null).ObservesProperty(() => SelectedItem);
        RemoveInvBookCommand = new DelegateCommand<LorId?>(RemoveInvBook);
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

    public string NewBookIdToAdd
    {
        get;
        set => SetProperty(ref field, value);
    } = "900001";

    // --- 命令 ---
    public DelegateCommand CreateCommand { get; }
    public DelegateCommand DeleteCommand { get; }
    public DelegateCommand AddWaveCommand { get; }
    public DelegateCommand<UnifiedWave> RemoveWaveCommand { get; }
    public DelegateCommand AddInvBookCommand { get; }
    public DelegateCommand<LorId?> RemoveInvBookCommand { get; }

    private void Delete()
    {
        if (SelectedItem != null && MessageBox.Show("确认删除?", "提示", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            Manager.StageRepo.Delete(SelectedItem);
            SelectedItem = null;
        }
    }

    private void AddWave() => SelectedItem?.AddWave();
    private void RemoveWave(UnifiedWave w) => SelectedItem?.RemoveWave(w);

    private void AddInvBook()
    {
        if (SelectedItem == null) return;

        // 限制最多 3 本 (虽然XML允许更多，但游戏UI通常只显示3本)
        if (SelectedItem.InvitationBooks.Count >= 3)
        {
            MessageBox.Show("邀请函书籍不能超过 3 本。");
            return;
        }

        LorId targetId = default;

        // 优先用下拉框选中的
        if (SelectedDropBook != null)
        {
            targetId = SelectedDropBook.GlobalId;
        }
        // 其次用手输的
        else if (!string.IsNullOrEmpty(NewBookIdToAdd))
        {
            targetId = new LorId(Manager.CurrentModId, NewBookIdToAdd.Trim());
        }

        if (!string.IsNullOrEmpty(targetId.ItemId) && !SelectedItem.InvitationBooks.Contains(targetId))
        {
            SelectedItem.AddInvitationBook(targetId);
            SelectedDropBook = null;// 清空选中
        }
    }

    private void RemoveInvBook(LorId? id)
    {
        if (id.HasValue) SelectedItem?.RemoveInvitationBook(id.Value);
    }
}