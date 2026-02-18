using System.Windows;
using Synthesis.Core;

namespace Synthesis.Feature.Keyword;

public class KeywordEditorViewModel : BindableBase
{
    public KeywordEditorViewModel(ProjectManager manager)
    {
        Manager = manager;
        CreateCommand = new DelegateCommand(delegate { Manager.KeywordRepo.Create(); });
        DeleteCommand = new DelegateCommand(Delete, () => SelectedItem != null).ObservesProperty(() => SelectedItem);
    }

    public ProjectManager Manager { get; }

    public UnifiedKeyword? SelectedItem
    {
        get;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand CreateCommand { get; }

    public DelegateCommand DeleteCommand { get; }

    private void Delete()
    {
        if (SelectedItem != null && MessageBox.Show("删除 [" + SelectedItem.Id + "]？", "提示", MessageBoxButton.YesNo) ==
            MessageBoxResult.Yes)
        {
            Manager.KeywordRepo.Delete(SelectedItem);
        }
    }
}
