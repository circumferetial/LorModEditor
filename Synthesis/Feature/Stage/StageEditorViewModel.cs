using System.Windows;
using Synthesis.Core;
using Synthesis.Feature.DropBook;

namespace Synthesis.Feature.Stage;

public class StageEditorViewModel : BindableBase
{
    public StageEditorViewModel(ProjectManager manager)
    {
        Manager = manager;
        CreateCommand = new DelegateCommand(delegate { Manager.StageRepo.Create(); });
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

    private void AddWave()
    {
        SelectedItem?.AddWave();
    }

    private void RemoveWave(UnifiedWave w)
    {
        SelectedItem?.RemoveWave(w);
    }

    private void AddInvBook()
    {
        if (SelectedItem == null)
        {
            return;
        }
        if (SelectedItem.InvitationBooks.Count >= 3)
        {
            MessageBox.Show("邀请函书籍不能超过 3 本。");
            return;
        }
        var lorId = default(LorId);
        if (SelectedDropBook != null)
        {
            lorId = SelectedDropBook.GlobalId;
        }
        else if (!string.IsNullOrEmpty(NewBookIdToAdd))
        {
            lorId = new LorId(Manager.CurrentModId, NewBookIdToAdd.Trim());
        }
        if (!string.IsNullOrEmpty(lorId.ItemId) && !SelectedItem.InvitationBooks.Contains(lorId))
        {
            SelectedItem.AddInvitationBook(lorId);
            SelectedDropBook = null;
        }
    }

    private void RemoveInvBook(LorId? id)
    {
        if (id.HasValue)
        {
            SelectedItem?.RemoveInvitationBook(id.Value);
        }
    }
}
