using System.Windows;
using LorModEditor.Core;

namespace LorModEditor.ViewModels;

public class PassiveEditorViewModel : BindableBase
{
    // 1. 构造函数注入
    public PassiveEditorViewModel(ProjectManager manager)
    {
        Manager = manager;

        CreateCommand = new DelegateCommand(CreatePassive);

        // DeleteCommand 只有在 SelectedItem 不为空时才可用
        // .ObservesProperty 会自动监听 SelectedItem 的变化并刷新按钮状态
        DeleteCommand = new DelegateCommand(DeletePassive, () => SelectedItem != null)
            .ObservesProperty(() => SelectedItem);
    }

    // 2. 数据源
    public ProjectManager Manager { get; }

    // 3. 选中项 (双向绑定)
    public UnifiedPassive? SelectedItem
    {
        get;
        set => SetProperty(ref field, value);
    }

    // 4. 命令
    public DelegateCommand CreateCommand { get; }
    public DelegateCommand DeleteCommand { get; }

    // --- 业务逻辑 ---

    private void CreatePassive()
    {
        try
        {
            Manager.PassiveRepo.Create();
            // 这里的选中逻辑通常由 CollectionView 或用户点击触发，
            // 如果需要自动选中新建项，可以在 Repo 改动后通过事件处理，或者简单地让用户去点。
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "创建失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeletePassive()
    {
        if (SelectedItem == null) return;

        if (MessageBox.Show($"确定要删除被动 [{SelectedItem.DisplayName}] 吗？", "提示", MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            Manager.PassiveRepo.Delete(SelectedItem);
            SelectedItem = null;// 删除后清空选中
        }
    }
}