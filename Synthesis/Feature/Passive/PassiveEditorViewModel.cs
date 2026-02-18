using System.Windows;
using Synthesis.Core;

namespace Synthesis.Feature.Passive;

public class PassiveEditorViewModel : BindableBase
{
    public PassiveEditorViewModel(ProjectManager manager)
    {
        Manager = manager;
        CreateCommand = new DelegateCommand(CreatePassive);
        DeleteCommand =
            new DelegateCommand(DeletePassive, () => SelectedItem != null).ObservesProperty(() => SelectedItem);
    }

    public ProjectManager Manager { get; }

    public UnifiedPassive? SelectedItem
    {
        get;
        set => SetProperty(ref field, value);
    }

    public DelegateCommand CreateCommand { get; }

    public DelegateCommand DeleteCommand { get; }

    private void CreatePassive()
    {
        try
        {
            Manager.PassiveRepo.Create();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "创建失败", MessageBoxButton.OK, MessageBoxImage.Hand);
        }
    }

    private void DeletePassive()
    {
        if (SelectedItem != null && MessageBox.Show("确定要删除被动 [" + SelectedItem.DisplayName + "] 吗？", "提示",
                MessageBoxButton.YesNo, MessageBoxImage.Exclamation) == MessageBoxResult.Yes)
        {
            Manager.PassiveRepo.Delete(SelectedItem);
            SelectedItem = null;
        }
    }
}
